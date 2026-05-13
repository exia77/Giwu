using Giwu.Contracts.Notifications;

namespace Giwu.Application.Notifications;

/// <summary>
/// Real-time push channel for newly-created notifications. The infrastructure
/// SaveChanges interceptor calls this after committing notification rows.
/// Implementations are wired in the Api project where SignalR lives.
/// </summary>
public interface INotificationBroadcaster
{
    Task BroadcastAsync(Guid recipientUserId, NotificationDto dto, CancellationToken ct = default);
}
