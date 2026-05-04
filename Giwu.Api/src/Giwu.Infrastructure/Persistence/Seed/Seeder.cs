using Giwu.Application.Common;
using Giwu.Domain.Identity;
using Giwu.Domain.Leaves;
using Giwu.Domain.Organization;
using Giwu.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Giwu.Infrastructure.Persistence.Seed;

/// <summary>
/// Idempotent seeding for built-in roles, default permissions, a demo tenant
/// and admin user. Safe to run on every startup.
/// </summary>
public static class Seeder
{
    public static async Task SeedAsync(
        ApplicationDbContext db,
        IPasswordHasher hasher,
        ITenantContext tenant,
        CancellationToken ct = default)
    {
        tenant.Bypass();

        // ── Demo tenant ─────────────────────────────────────────────────────
        var demo = await db.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Name == "Giwu Demo", ct);
        if (demo is null)
        {
            demo = new Tenant
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Name = "Giwu Demo",
                LegalName = "Giwu Inc.",
            };
            db.Tenants.Add(demo);
            await db.SaveChangesAsync(ct);
        }

        tenant.SetTenant(demo.Id);

        // ── System roles + default permission map ───────────────────────────
        var defaults = new Dictionary<string, string[]>
        {
            [SystemRoles.HrAdmin]      = Permissions.All.ToArray(),
            [SystemRoles.HrSpecialist] = new[]
            {
                Permissions.Employees.View, Permissions.Employees.Manage,
                Permissions.Departments.View, Permissions.Departments.Manage,
                Permissions.Attendance.ViewAll,
                Permissions.Leaves.ViewAll, Permissions.Leaves.Approve, Permissions.Leaves.Manage,
                Permissions.Recruitment.View, Permissions.Recruitment.Manage,
                Permissions.Benefits.View,
                Permissions.Reports.View, Permissions.Reports.Run,
                Permissions.Settings.View,
            },
            [SystemRoles.Manager] = new[]
            {
                Permissions.Employees.View,
                Permissions.Attendance.ViewAll,
                Permissions.Leaves.ViewAll, Permissions.Leaves.Approve,
                Permissions.Reports.View,
            },
            [SystemRoles.Finance] = new[]
            {
                Permissions.Payroll.Run, Permissions.Payroll.Approve, Permissions.Payroll.ViewAll,
                Permissions.Benefits.View, Permissions.Benefits.Manage,
                Permissions.Reports.View, Permissions.Reports.Run,
            },
            [SystemRoles.Employee] = new[]
            {
                Permissions.Attendance.ViewSelf,
                Permissions.Leaves.FileSelf,
            },
        };

        foreach (var (name, perms) in defaults)
        {
            var role = await db.Roles.FirstOrDefaultAsync(r => r.Name == name, ct);
            if (role is null)
            {
                role = new Role { Name = name, IsSystem = true, TenantId = demo.Id };
                db.Roles.Add(role);
                await db.SaveChangesAsync(ct);
            }

            var existing = await db.RolePermissions
                .Where(p => p.RoleId == role.Id).Select(p => p.PermissionKey).ToListAsync(ct);
            foreach (var p in perms.Except(existing))
                db.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionKey = p });
        }
        await db.SaveChangesAsync(ct);

        // ── Demo HR Admin user ──────────────────────────────────────────────
        var admin = await db.Users.FirstOrDefaultAsync(u => u.Email == "admin@giwu.ph", ct);
        if (admin is null)
        {
            var hrAdminRole = await db.Roles.FirstAsync(r => r.Name == SystemRoles.HrAdmin, ct);
            admin = new User
            {
                Email        = "admin@giwu.ph",
                PasswordHash = hasher.Hash("ChangeMe!123"),
                DisplayName  = "Demo Admin",
                IsActive     = true,
                TenantId     = demo.Id,
            };
            admin.Roles.Add(new UserRoleAssignment { RoleId = hrAdminRole.Id });
            db.Users.Add(admin);
            await db.SaveChangesAsync(ct);
        }

        // ── Demo department + leave types ───────────────────────────────────
        if (!await db.Departments.AnyAsync(ct))
        {
            db.Departments.Add(new Department { Name = "Human Resources", Code = "HR" });
            db.Departments.Add(new Department { Name = "Engineering",     Code = "ENG" });
            db.Departments.Add(new Department { Name = "Finance",         Code = "FIN" });
            await db.SaveChangesAsync(ct);
        }

        if (!await db.LeaveTypes.AnyAsync(ct))
        {
            db.LeaveTypes.Add(new LeaveType { Name = "Vacation Leave", Code = "VL", Category = LeaveCategory.Vacation,    AnnualEntitlementDays = 15, AccrualMode = LeaveAccrualMode.Yearly });
            db.LeaveTypes.Add(new LeaveType { Name = "Sick Leave",     Code = "SL", Category = LeaveCategory.Sick,        AnnualEntitlementDays = 15, AccrualMode = LeaveAccrualMode.Yearly });
            db.LeaveTypes.Add(new LeaveType { Name = "Service Incentive Leave", Code = "SIL", Category = LeaveCategory.Sil, IsMandated = true, AnnualEntitlementDays = 5, AccrualMode = LeaveAccrualMode.Tenure, LegalReference = "Labor Code Art. 95" });
            await db.SaveChangesAsync(ct);
        }
    }
}
