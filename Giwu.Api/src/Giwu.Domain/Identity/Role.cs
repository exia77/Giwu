using Giwu.Domain.Common;

namespace Giwu.Domain.Identity;

public class Role : AuditableEntity
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public bool IsSystem { get; set; }
    public List<RolePermission> Permissions { get; set; } = new();
}

public class RolePermission
{
    public Guid RoleId { get; set; }
    public string PermissionKey { get; set; } = "";
}

/// <summary>
/// Stable permission keys. The role/permission table holds rows keyed by these
/// strings. Endpoints declare <c>[Authorize(Policy = Permissions.Payroll.Run)]</c>.
/// </summary>
public static class Permissions
{
    public static class Employees
    {
        public const string View   = "employees.view";
        public const string Manage = "employees.manage";
    }
    public static class Departments
    {
        public const string View   = "departments.view";
        public const string Manage = "departments.manage";
    }
    public static class Attendance
    {
        public const string ViewSelf = "attendance.view.self";
        public const string ViewAll  = "attendance.view.all";
        public const string Manage   = "attendance.manage";
    }
    public static class Leaves
    {
        public const string FileSelf = "leave.request.file";
        public const string ViewAll  = "leave.request.view.all";
        public const string Approve  = "leave.request.approve";
        public const string Manage   = "leave.manage";
    }
    public static class Payroll
    {
        public const string Run     = "payroll.run.create";
        public const string Approve = "payroll.run.approve";
        public const string ViewAll = "payroll.payslip.view.all";
    }
    public static class Benefits
    {
        public const string View   = "benefits.view";
        public const string Manage = "benefits.manage";
    }
    public static class Recruitment
    {
        public const string View   = "recruitment.view";
        public const string Manage = "recruitment.manage";
    }
    public static class Reports
    {
        public const string View = "reports.view";
        public const string Run  = "reports.run";
    }
    public static class Settings
    {
        public const string View   = "settings.view";
        public const string Manage = "settings.manage";
    }

    public static IEnumerable<string> All => new[]
    {
        Employees.View, Employees.Manage,
        Departments.View, Departments.Manage,
        Attendance.ViewSelf, Attendance.ViewAll, Attendance.Manage,
        Leaves.FileSelf, Leaves.ViewAll, Leaves.Approve, Leaves.Manage,
        Payroll.Run, Payroll.Approve, Payroll.ViewAll,
        Benefits.View, Benefits.Manage,
        Recruitment.View, Recruitment.Manage,
        Reports.View, Reports.Run,
        Settings.View, Settings.Manage,
    };
}

public static class SystemRoles
{
    public const string HrAdmin       = "HrAdmin";
    public const string HrSpecialist  = "HrSpecialist";
    public const string Manager       = "Manager";
    public const string Finance       = "Finance";
    public const string Employee      = "Employee";
}
