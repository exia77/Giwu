using Giwu.Domain.Common;

namespace Giwu.Domain.Identity;

public class User : AuditableEntity
{
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public Guid? EmployeeId { get; set; }
    public bool IsActive { get; set; } = true;
    public bool MfaEnabled { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
    public int FailedLoginCount { get; set; }
    public DateTimeOffset? LockedUntil { get; set; }

    public List<UserRoleAssignment> Roles { get; set; } = new();
}

public class UserRoleAssignment
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    public Role Role { get; set; } = null!;
}

public class RefreshToken : AuditableEntity
{
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = "";
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public string? RevokedReason { get; set; }
}
