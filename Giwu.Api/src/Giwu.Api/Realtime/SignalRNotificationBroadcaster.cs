using Giwu.Application.Notifications;
using Giwu.Contracts.Notifications;
using Microsoft.AspNetCore.SignalR;

namespace Giwu.Api.Realtime;

/// <summary>
/// Pushes notifications to the recipient's active SignalR connection(s)
/// using Clients.User(...) — routing handled by JwtUserIdProvider.
/// </summary>
public sealed class SignalRNotificationBroadcaster(IHubContext<NotificationsHub> hub)
    : INotificationBroadcaster
{
    public Task BroadcastAsync(Guid recipientUserId, NotificationDto dto, CancellationToken ct = default) =>
        hub.Clients.User(recipientUserId.ToString()).SendAsync("notification", dto, ct);
}
