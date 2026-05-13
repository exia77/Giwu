namespace Giwu.HRMS.Hybrid.Services;

/// <summary>
/// The five roles used across the HR system. A user has exactly one primary role.
/// Permissions are layered on top via the RoleAuthService permission map.
/// </summary>
public enum UserRole
{
    HrAdmin,        // Full access — owns Settings and the role/permission matrix
    HrSpecialist,   // Most HR pages, but read-only on Settings, no Security/Roles
    Manager,        // Team-level access: approve leave, view team attendance + people
    Finance,        // Payroll, Reports, Compliance — no Recruitment or Benefits admin
    Employee,       // Self-service only: own payslips, balances, file requests
}

/// <summary>
/// Stable identifiers for each navigable area. The nav menu in DashboardLayout
/// references these. New pages add a new key here and a row to DefaultPermissions.
/// </summary>
public static class NavKeys
{
    public const string Dashboard      = "dashboard";
    public const string Employees      = "employees";
    public const string Departments    = "departments";
    public const string Attendance     = "attendance";
    public const string Recruitment    = "recruitment";
    public const string Payroll        = "payroll";
    public const string Benefits       = "benefits";
    public const string Leave          = "leave";
    public const string Reports        = "reports";
    public const string Settings       = "settings";

    /// <summary>All keys, ordered as they appear in the sidebar. Used by the
    /// permissions-matrix UI to render rows in a stable order.</summary>
    public static readonly string[] All = new[]
    {
        Dashboard, Employees, Departments, Attendance, Recruitment,
        Payroll, Benefits, Leave, Reports, Settings,
    };

    public static string Label(string key) => key switch
    {
        Dashboard   => "Dashboard",
        Employees   => "Employees",
        Departments => "Departments",
        Attendance  => "Attendance",
        Recruitment => "Recruitment",
        Payroll     => "Payroll",
        Benefits    => "Benefits",
        Leave       => "Leave & Time Off",
        Reports     => "Reports",
        Settings    => "Settings",
        _ => key
    };

    public static string Route(string key) => key switch
    {
        Dashboard   => "/",
        Employees   => "/employees",
        Departments => "/departments",
        Attendance  => "/attendance",
        Recruitment => "/recruitment",
        Payroll     => "/payroll",
        Benefits    => "/benefits",
        Leave       => "/leave",
        Reports     => "/reports",
        Settings    => "/settings",
        _ => "/"
    };
}

/// <summary>
/// Represents a user with a role assignment. In a production system this would be
/// hydrated from your auth provider (Entra/Auth0/etc) — for the demo we keep it on
/// the service.
/// </summary>
public class UserSession
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Initials { get; set; } = "";
    public string AvatarColor { get; set; } = "primary"; // matches the AvatarStyle helpers
    public UserRole Role { get; set; }
    public string Department { get; set; } = "";
    public string JobTitle { get; set; } = "";
}

/// <summary>
/// Central authority for role-based UI access. Inject as a singleton and call:
///   • CanSee(NavKeys.Payroll) from the sidebar to filter links
///   • CanAccess(navKey) from inside a page's OnInitialized to gate by URL
///   • CurrentUser to render the user's identity in the footer / topbar
///
/// The permission matrix can be edited at runtime through the Settings UI; OnChange
/// fires whenever the matrix or the active user changes so subscribers can re-render.
///
/// Register in Program.cs:
///     builder.Services.AddSingleton&lt;RoleAuthService&gt;();
/// </summary>
public class RoleAuthService : IRoleAuthService
{
    /// <summary>
    /// Permission matrix: role → set of nav keys that role can access.
    /// Mutable so the Settings UI can update it. Defaults to the table below
    /// which encodes typical PH HR responsibilities.
    /// </summary>
    public Dictionary<UserRole, HashSet<string>> Permissions { get; } = DefaultPermissions();

    /// <summary>The available demo users, used by the topbar role-switcher.</summary>
    public List<UserSession> AvailableUsers { get; } = SeedUsers();

    /// <summary>Currently signed-in user. Switching this fires OnChange.</summary>
    public UserSession CurrentUser { get; private set; }

    public event Action? OnChange;

    public RoleAuthService()
    {
        CurrentUser = AvailableUsers[0]; // Default to HR Admin (Jane)
    }

    public void SetCurrentUser(string userId)
    {
        var match = AvailableUsers.FirstOrDefault(u => u.Id == userId);
        if (match == null) return;
        CurrentUser = match;
        OnChange?.Invoke();
    }

    /// <summary>
    /// Replaces the current user with a profile pulled from the API. Maps the
    /// first matching server role name to a <see cref="UserRole"/>; defaults to
    /// Employee if no system role matches. Permissions list from the JWT is
    /// kept for future fine-grained checks (TODO: switch CanSee to consult it).
    /// </summary>
    public void SetCurrentUserFromApi(
        string id, string displayName, string email,
        IEnumerable<string> roles, IEnumerable<string> permissions)
    {
        var roleEnum = roles
            .Select(r => Enum.TryParse<UserRole>(r, out var v) ? (UserRole?)v : null)
            .FirstOrDefault(r => r is not null) ?? UserRole.Employee;

        CurrentUser = new UserSession
        {
            Id          = id,
            Name        = string.IsNullOrWhiteSpace(displayName) ? email : displayName,
            Email       = email,
            Initials    = MakeInitials(displayName, email),
            AvatarColor = "primary",
            Role        = roleEnum,
            Department  = "",
            JobTitle    = RoleLabel(roleEnum),
        };

        ApiPermissions = permissions.ToHashSet(StringComparer.Ordinal);
        OnChange?.Invoke();
    }

    /// <summary>Permission keys received from the JWT (e.g. "leave.request.approve").</summary>
    public HashSet<string> ApiPermissions { get; private set; } = new();

    private static string MakeInitials(string displayName, string email)
    {
        var src = string.IsNullOrWhiteSpace(displayName) ? email : displayName;
        if (string.IsNullOrWhiteSpace(src)) return "?";
        var parts = src.Split(new[] { ' ', '@', '.' }, StringSplitOptions.RemoveEmptyEntries);
        return parts.Length switch
        {
            0 => "?",
            1 => parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant(),
            _ => $"{parts[0][0]}{parts[1][0]}".ToUpperInvariant()
        };
    }

    /// <summary>
    /// Returns true if the current user's role is allowed to see the given nav key.
    /// </summary>
    public bool CanSee(string navKey) =>
        Permissions.TryGetValue(CurrentUser.Role, out var allowed) && allowed.Contains(navKey);

    /// <summary>
    /// Same as CanSee but named to emphasize URL-level authorization. Use this in
    /// page OnInitialized to redirect unauthorized users away.
    /// </summary>
    public bool CanAccess(string navKey) => CanSee(navKey);

    /// <summary>
    /// Toggles a permission for a role and broadcasts the change. The Settings
    /// UI calls this when an admin edits the matrix.
    /// </summary>
    public void TogglePermission(UserRole role, string navKey, bool granted)
    {
        if (!Permissions.ContainsKey(role))
            Permissions[role] = new HashSet<string>();

        if (granted) Permissions[role].Add(navKey);
        else         Permissions[role].Remove(navKey);

        OnChange?.Invoke();
    }

    public void ResetPermissionsToDefaults()
    {
        var defaults = DefaultPermissions();
        Permissions.Clear();
        foreach (var kv in defaults) Permissions[kv.Key] = kv.Value;
        OnChange?.Invoke();
    }

    public static string RoleLabel(UserRole role) => role switch
    {
        UserRole.HrAdmin       => "HR Admin",
        UserRole.HrSpecialist  => "HR Specialist",
        UserRole.Manager       => "Manager",
        UserRole.Finance       => "Finance / Payroll",
        UserRole.Employee      => "Employee",
        _ => "—"
    };

    public static string RoleDescription(UserRole role) => role switch
    {
        UserRole.HrAdmin       => "Full system access including settings and the role matrix.",
        UserRole.HrSpecialist  => "Day-to-day HR operations across employees, leave, recruitment, and benefits.",
        UserRole.Manager       => "Approves their team's leave and views team attendance and headcount.",
        UserRole.Finance       => "Owns payroll runs, statutory remittances, and compliance reporting.",
        UserRole.Employee      => "Self-service access to own payslips, leave balances, and benefit claims.",
        _ => ""
    };

    public static string RoleBadgeClass(UserRole role) => role switch
    {
        UserRole.HrAdmin       => "role-badge-admin",
        UserRole.HrSpecialist  => "role-badge-hr",
        UserRole.Manager       => "role-badge-manager",
        UserRole.Finance       => "role-badge-finance",
        UserRole.Employee      => "role-badge-employee",
        _ => ""
    };

    /// <summary>
    /// Default permission matrix. This is the rule book — edit here to change
    /// the baseline shipped configuration.
    /// </summary>
    private static Dictionary<UserRole, HashSet<string>> DefaultPermissions() => new()
    {
        // HR Admin — everything
        [UserRole.HrAdmin] = new HashSet<string>(NavKeys.All),

        // HR Specialist — everything except Settings (no permission to edit the org config or roles)
        [UserRole.HrSpecialist] = new HashSet<string>
        {
            NavKeys.Dashboard, NavKeys.Employees, NavKeys.Departments,
            NavKeys.Attendance, NavKeys.Recruitment, NavKeys.Benefits,
            NavKeys.Leave, NavKeys.Reports,
        },

        // Manager — team-level visibility, leave approval, no Payroll/Settings/Recruitment
        [UserRole.Manager] = new HashSet<string>
        {
            NavKeys.Dashboard, NavKeys.Employees,
            NavKeys.Attendance, NavKeys.Leave, NavKeys.Reports,
        },

        // Finance — payroll-centric responsibilities
        [UserRole.Finance] = new HashSet<string>
        {
            NavKeys.Dashboard, NavKeys.Payroll, NavKeys.Benefits, NavKeys.Reports,
        },

        // Employee — self-service only. They land on Dashboard and use Leave to file requests.
        [UserRole.Employee] = new HashSet<string>
        {
            NavKeys.Dashboard, NavKeys.Leave,
        },
    };

    private static List<UserSession> SeedUsers() => new()
    {
        new() { Id="U001", Name="Jane Dela Cruz",   Email="jane@giwu.ph",  Initials="JD", AvatarColor="primary",
                Role=UserRole.HrAdmin,      Department="HR",          JobTitle="HR Manager" },
        new() { Id="U002", Name="Nadia Lim",        Email="nadia@giwu.ph", Initials="NL", AvatarColor="green",
                Role=UserRole.HrSpecialist, Department="HR",          JobTitle="HR Specialist" },
        new() { Id="U003", Name="Marco Cruz",       Email="marco@giwu.ph", Initials="MC", AvatarColor="blue",
                Role=UserRole.Manager,      Department="Product",     JobTitle="Product Manager" },
        new() { Id="U004", Name="Liza Mendoza",     Email="liza@giwu.ph",  Initials="LM", AvatarColor="amber",
                Role=UserRole.Finance,      Department="Finance",     JobTitle="Payroll Specialist" },
        new() { Id="U005", Name="Joan Dela Rosa",   Email="joan@giwu.ph",  Initials="JD", AvatarColor="green",
                Role=UserRole.Employee,     Department="Engineering", JobTitle="Software Engineer" },
    };
}
