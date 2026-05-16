using FluentValidation;
using Giwu.Application.Common;
using Giwu.Contracts.Auth;
using Giwu.Domain.Identity;
using Giwu.Domain.Leaves;
using Giwu.Domain.Organization;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Giwu.Application.Auth.Google;

public sealed record GoogleSignInCommand(string IdToken)
    : IRequest<Result<LoginResponse>>;

public sealed class GoogleSignInValidator : AbstractValidator<GoogleSignInCommand>
{
    public GoogleSignInValidator()
    {
        RuleFor(x => x.IdToken).NotEmpty();
    }
}

internal sealed class GoogleSignInHandler(
    IApplicationDbContext db,
    IGoogleTokenVerifier verifier,
    IJwtTokenService jwt,
    ITenantContext tenant,
    TimeProvider clock)
    : IRequestHandler<GoogleSignInCommand, Result<LoginResponse>>
{
    public async Task<Result<LoginResponse>> Handle(GoogleSignInCommand cmd, CancellationToken ct)
    {
        var google = await verifier.VerifyAsync(cmd.IdToken, ct);
        if (google is null || string.IsNullOrEmpty(google.Email))
            return Result<LoginResponse>.Forbidden("Google sign-in failed");

        if (!google.EmailVerified)
            return Result<LoginResponse>.Forbidden("Your Google email is not verified");

        tenant.Bypass();

        var user = await db.Users
            .Include(u => u.Roles).ThenInclude(r => r.Role).ThenInclude(r => r.Permissions)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == google.Email && u.DeletedAt == null, ct);

        // First Google sign-in for this email → auto-provision a fresh user under
        // the default tenant with the Employee role. The user can later be linked
        // to an Employee record by HR; until then they have self-service access only.
        if (user is null)
        {
            var provisioned = await TryProvisionAsync(google, ct);
            if (provisioned.error is not null) return Result<LoginResponse>.Forbidden(provisioned.error);
            user = provisioned.user!;
        }

        if (!user.IsActive)
            return Result<LoginResponse>.Forbidden("Your account is disabled — contact HR.");

        // Backfill an Employee record for older auto-provisioned users that
        // were created before this step existed. Without one, clock-in / file-
        // leave / etc. would 403 because the JWT has no "eid" claim.
        if (user.EmployeeId is null)
        {
            tenant.SetTenant(user.TenantId);
            var emp = await ProvisionEmployeeAsync(google, user.TenantId, ct);
            user.EmployeeId = emp.Id;
        }

        user.FailedLoginCount = 0;
        user.LockedUntil = null;
        user.LastLoginAt = clock.GetUtcNow();

        var roles = user.Roles.Select(r => r.Role.Name).ToArray();
        var perms = user.Roles
            .SelectMany(r => r.Role.Permissions.Select(p => p.PermissionKey))
            .Distinct()
            .ToArray();

        tenant.SetTenant(user.TenantId);

        var (access, exp) = jwt.IssueAccessToken(
            user.Id, user.TenantId, user.EmployeeId,
            user.Email, user.DisplayName, roles, perms);

        var refresh = jwt.IssueRefreshToken();
        db.RefreshTokens.Add(new RefreshToken
        {
            UserId    = user.Id,
            TenantId  = user.TenantId,
            TokenHash = jwt.HashRefreshToken(refresh),
            ExpiresAt = clock.GetUtcNow().AddDays(7),
        });
        await db.SaveChangesAsync(ct);

        return Result<LoginResponse>.Success(new LoginResponse(
            access, refresh, exp,
            new UserMeDto(user.Id, user.TenantId, user.EmployeeId,
                          user.Email, user.DisplayName, roles, perms)));
    }

    /// <summary>
    /// Creates a brand-new <see cref="User"/> from a verified Google profile and
    /// assigns the Employee role. Picks the first available tenant (single-tenant
    /// deployments today). Returns an error string if the system isn't seeded
    /// (no tenants, no Employee role) — the caller surfaces it to the client.
    /// </summary>
    private async Task<(User? user, string? error)> TryProvisionAsync(
        GoogleUserInfo google, CancellationToken ct)
    {
        // For now, all auto-provisioned users land on the demo/default tenant.
        // Multi-tenant SaaS would resolve the tenant from the email domain or an invite code.
        var defaultTenant = await db.Tenants.IgnoreQueryFilters()
            .OrderBy(t => t.CreatedAt)
            .FirstOrDefaultAsync(ct);
        if (defaultTenant is null) return (null, "No tenant configured to provision Google users into.");

        tenant.SetTenant(defaultTenant.Id);

        var employeeRole = await db.Roles
            .Include(r => r.Permissions)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.Name == SystemRoles.Employee && r.TenantId == defaultTenant.Id, ct);
        if (employeeRole is null) return (null, "Employee role is missing — seed the database first.");

        var user = new User
        {
            Email        = google.Email,
            DisplayName  = string.IsNullOrWhiteSpace(google.DisplayName) ? google.Email : google.DisplayName,
            PasswordHash = "",          // Google-only account; password login is blocked
            IsActive     = true,
            TenantId     = defaultTenant.Id,
        };
        user.Roles.Add(new UserRoleAssignment { Role = employeeRole, RoleId = employeeRole.Id });
        db.Users.Add(user);

        // Auto-provision an Employee record too. Without one, downstream actions
        // that key off the JWT's "eid" claim (clock in/out, file leave, view
        // payslips) will 403. HR can edit the placeholder data later via the
        // Employees page — we just need a row to exist so the JWT picks it up.
        var employee = await ProvisionEmployeeAsync(google, defaultTenant.Id, ct);
        user.EmployeeId = employee.Id;

        // Persist now so subsequent role/permission projection reads see a consistent
        // user — the access-token issuance code below relies on user.Id being set.
        await db.SaveChangesAsync(ct);
        return (user, null);
    }

    /// <summary>
    /// Creates a placeholder Employee record for an auto-provisioned Google user.
    /// Splits the Google display name into First/Last, picks the first available
    /// department in the tenant (or creates an "Unassigned" department if the
    /// tenant has none yet), and assigns a generated employee number derived
    /// from a short prefix of the User id — guaranteed unique without a counter.
    /// </summary>
    private async Task<Employee> ProvisionEmployeeAsync(
        GoogleUserInfo google, Guid tenantId, CancellationToken ct)
    {
        // Resolve a department. Prefer an existing one to avoid clutter; fall
        // back to creating "Unassigned" so the FK is satisfied even on a
        // freshly-seeded tenant.
        var dept = await db.Departments.OrderBy(d => d.CreatedAt).FirstOrDefaultAsync(ct);
        if (dept is null)
        {
            dept = new Department
            {
                Name = "Unassigned",
                Code = "UNASSIGNED",
                TenantId = tenantId,
            };
            db.Departments.Add(dept);
            await db.SaveChangesAsync(ct);
        }

        var (first, last) = SplitName(google.DisplayName, google.Email);

        // Unique employee number derived from the email's local-part stem and a
        // 6-char id prefix. Doesn't clash with the seeded "EMP-1001…" pattern.
        var stem = (google.Email.Split('@')[0] ?? "user").ToUpperInvariant();
        if (stem.Length > 6) stem = stem[..6];
        var idPart = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpperInvariant();

        var emp = new Employee
        {
            EmployeeNumber = $"G-{stem}-{idPart}",
            FirstName      = first,
            LastName       = last,
            Email          = google.Email,
            DepartmentId   = dept.Id,
            JobTitle       = "—",
            HireDate       = DateOnly.FromDateTime(DateTime.UtcNow.Date),
            Status         = EmploymentStatus.Active,
            EmploymentType = EmploymentType.Regular,
            TenantId       = tenantId,
        };
        db.Employees.Add(emp);
        await db.SaveChangesAsync(ct);

        // Seed a LeaveBalance row per active leave type for this year so the
        // File Leave dialog can show real "X days available" numbers from the
        // very first sign-in. Without this, the preview is blank until the
        // user files something (which lazily creates a balance on first use).
        var activeTypes = await db.LeaveTypes
            .Where(t => t.TenantId == tenantId && t.IsActive)
            .ToListAsync(ct);

        var currentYear = DateTime.UtcNow.Year;
        foreach (var lt in activeTypes)
        {
            db.LeaveBalances.Add(new LeaveBalance
            {
                EmployeeId  = emp.Id,
                LeaveTypeId = lt.Id,
                PeriodYear  = currentYear,
                Entitlement = lt.AnnualEntitlementDays,
                CarryOver   = 0m,
                Used        = 0m,
                Pending     = 0m,
                TenantId    = tenantId,
            });
        }
        if (activeTypes.Count > 0) await db.SaveChangesAsync(ct);

        return emp;
    }

    private static (string First, string Last) SplitName(string? displayName, string emailFallback)
    {
        var src = string.IsNullOrWhiteSpace(displayName) ? emailFallback.Split('@')[0] : displayName.Trim();
        var parts = src.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return ("New", "Employee");
        if (parts.Length == 1) return (parts[0], "—");
        return (parts[0], string.Join(' ', parts.Skip(1)));
    }
}
