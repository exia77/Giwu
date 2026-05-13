using Giwu.Application.Common;
using Giwu.Domain.Identity;
using Giwu.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Giwu.Infrastructure.Persistence.Seed;

/// <summary>
/// Bare-minimum bootstrap so the app can boot and accept login.
/// Creates:
///   1. A single tenant (named "Tenant"; rename via Settings → Branding)
///   2. The 5 system roles + their default permission maps
///   3. One admin user (admin@giwu.ph / ChangeMe!123) with HrAdmin role
/// Nothing else. Departments, leave types, employees, attendance, etc.
/// are created by the operator through the UI.
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

        // ── Tenant (one fixed-id row so existing references stay stable) ──────
        var demo = await db.Tenants.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == Guid.Parse("11111111-1111-1111-1111-111111111111"), ct);
        if (demo is null)
        {
            demo = new Tenant
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Name = "Tenant",
            };
            db.Tenants.Add(demo);
            await db.SaveChangesAsync(ct);
        }

        tenant.SetTenant(demo.Id);

        // ── System roles + default permission map ────────────────────────────
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

        // ── Bootstrap admin user (so first-time login works) ─────────────────
        var admin = await db.Users.FirstOrDefaultAsync(u => u.Email == "admin@giwu.ph", ct);
        if (admin is null)
        {
            var hrAdminRole = await db.Roles.FirstAsync(r => r.Name == SystemRoles.HrAdmin, ct);
            admin = new User
            {
                Email        = "admin@giwu.ph",
                PasswordHash = hasher.Hash("ChangeMe!123"),
                DisplayName  = "Admin",
                IsActive     = true,
                TenantId     = demo.Id,
            };
            admin.Roles.Add(new UserRoleAssignment { RoleId = hrAdminRole.Id });
            db.Users.Add(admin);
            await db.SaveChangesAsync(ct);
        }
    }
}
