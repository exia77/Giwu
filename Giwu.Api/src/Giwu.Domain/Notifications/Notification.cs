using Giwu.Domain.Common;

namespace Giwu.Domain.Notifications;

public enum NotificationType
{
    LeaveFiled        = 0,
    LeaveApproved     = 1,
    LeaveRejected     = 2,
    OvertimeFiled     = 3,
    OvertimeApproved  = 4,
    OvertimeRejected  = 5,
    AttendanceFlagged = 6,
    Generic           = 99,
}

/// <summary>
/// In-app notification surfaced via the bell icon in the topbar.
/// Owned by a recipient User (not Employee) so that admins and finance
/// users without an Employee record can still receive notifications.
/// </summary>
public class Notification : AuditableEntity
{
    public Guid RecipientUserId { get; set; }
    public NotificationType Type { get; set; }
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";

    // Optional pointer to the entity that triggered this notification, so
    // clicking the row can deep-link (e.g., RelatedEntityType="LeaveRequest"
    // + RelatedEntityId=<guid> → /leave/{id}).
    public Guid? RelatedEntityId { get; set; }
    public string? RelatedEntityType { get; set; }

    public DateTimeOffset? ReadAt { get; set; }
    public bool IsRead => ReadAt.HasValue;
}
