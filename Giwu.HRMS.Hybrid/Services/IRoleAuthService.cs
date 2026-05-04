namespace Giwu.HRMS.Hybrid.Services;

public interface IRoleAuthService
{
    Dictionary<UserRole, HashSet<string>> Permissions { get; }
    List<UserSession> AvailableUsers { get; }
    UserSession CurrentUser { get; }
    event Action? OnChange;

    void SetCurrentUser(string userId);
    bool CanSee(string navKey);
    bool CanAccess(string navKey);
    void TogglePermission(UserRole role, string navKey, bool granted);
    void ResetPermissionsToDefaults();
}
