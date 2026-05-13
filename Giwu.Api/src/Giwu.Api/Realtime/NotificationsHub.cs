using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Giwu.Api.Realtime;

/// <summary>
/// SignalR hub that pushes in-app notifications to the recipient in real
/// time. Clients connect with their JWT and SignalR identifies them by the
/// "sub" claim (user id GUID). The server-side broadcast uses
/// IHubContext.Clients.User(userId) — see NotificationBroadcastInterceptor.
/// </summary>
[Authorize]
public sealed class NotificationsHub : Hub
{
    public override Task OnConnectedAsync()
    {
        // No groups needed — Clients.User(userId) handles routing per-user
        // via JwtUserIdProvider below.
        return base.OnConnectedAsync();
    }
}

/// <summary>
/// Tells SignalR which connection belongs to which user. Defaults look at
/// NameIdentifier; our JWT uses "sub" (configured with MapInboundClaims=false
/// in Program.cs), so we read that claim directly.
/// </summary>
public sealed class JwtUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        var user = connection.User;
        return user.FindFirst("sub")?.Value
            ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
}
