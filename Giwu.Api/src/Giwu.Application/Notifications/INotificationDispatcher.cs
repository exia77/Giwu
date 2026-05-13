using Giwu.Domain.Notifications;

namespace Giwu.Application.Notifications;

/// <summary>
/// Internal helper for emitting in-app notifications from inside command
/// handlers. Stages the entity on the DbContext but doesn't call SaveChanges
/// — callers commit as part of their own transaction so the notification
/// row appears atomically with the entity it refers to.
/// </summary>
public interface INotificationDispatcher
{
    /// <summary>Notifies a specific user.</summary>
    Task NotifyUserAsync(
        Guid recipientUserId,
        NotificationType type,
        string title,
        string body,
        Guid? relatedEntityId = null,
        string? relatedEntityType = null,
        CancellationToken ct = default);

    /// <summary>Notifies every user holding a given role within the current tenant.</summary>
    Task NotifyRoleAsync(
        string roleName,
        NotificationType type,
        string title,
        string body,
        Guid? relatedEntityId = null,
        string? relatedEntityType = null,
        CancellationToken ct = default);
}
