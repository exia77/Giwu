using Giwu.Application.Common;
using Giwu.Application.Notifications;
using Giwu.Domain.Identity;
using Giwu.Domain.Notifications;
using Giwu.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Giwu.Infrastructure.Notifications;

/// <summary>
/// Routes notifications through the current tenant's NotificationSettings.
/// Each call: looks up the recipient + tenant, decides channel (InApp, Email,
/// Both, Off) based on the NotificationType, then stages an in-app row and/or
/// queues an email accordingly. Persistence happens on the caller's
/// SaveChanges; emails are sent fire-and-forget after staging.
/// </summary>
internal sealed class NotificationDispatcher(
    IApplicationDbContext db,
    ITenantContext tenant,
    IEmailSender email,
    ILogger<NotificationDispatcher> log)
    : INotificationDispatcher
{
    public async Task NotifyUserAsync(
        Guid recipientUserId,
        NotificationType type,
        string title,
        string body,
        Guid? relatedEntityId = null,
        string? relatedEntityType = null,
        CancellationToken ct = default)
    {
        var channel = await ResolveChannelAsync(type, ct);
        if (channel == NotificationChannel.Off) return;

        if (channel is NotificationChannel.InApp or NotificationChannel.Both)
        {
            db.Notifications.Add(new Notification
            {
                RecipientUserId   = recipientUserId,
                Type              = type,
                Title             = title,
                Body              = body,
                RelatedEntityId   = relatedEntityId,
                RelatedEntityType = relatedEntityType,
            });
        }

        if (channel is NotificationChannel.Email or NotificationChannel.Both)
            await TrySendEmailAsync(recipientUserId, title, body, ct);
    }

    public async Task NotifyRoleAsync(
        string roleName,
        NotificationType type,
        string title,
        string body,
        Guid? relatedEntityId = null,
        string? relatedEntityType = null,
        CancellationToken ct = default)
    {
        var channel = await ResolveChannelAsync(type, ct);
        if (channel == NotificationChannel.Off) return;

        var role = await db.Roles.FirstOrDefaultAsync(r => r.Name == roleName, ct);
        if (role is null) return;

        // Materialize the recipients once (Id + Email) so we can fan out
        // to in-app rows and emails without re-querying.
        var recipients = await db.Users
            .Where(u => u.IsActive && u.Roles.Any(ra => ra.RoleId == role.Id))
            .Select(u => new { u.Id, u.Email, u.DisplayName })
            .ToListAsync(ct);

        foreach (var u in recipients)
        {
            if (channel is NotificationChannel.InApp or NotificationChannel.Both)
            {
                db.Notifications.Add(new Notification
                {
                    RecipientUserId   = u.Id,
                    Type              = type,
                    Title             = title,
                    Body              = body,
                    RelatedEntityId   = relatedEntityId,
                    RelatedEntityType = relatedEntityType,
                });
            }

            if (channel is NotificationChannel.Email or NotificationChannel.Both)
                await TrySendEmailDirectAsync(u.Email, u.DisplayName, title, body, ct);
        }
    }

    // ── Internals ──────────────────────────────────────────────────────────

    /// <summary>Looks up the tenant's NotificationSettings (no-tracking,
    /// includes the owned NotificationSettings via the navigation), and maps
    /// the requested NotificationType to one of the settings fields. Returns
    /// a sensible default if anything's missing.</summary>
    private async Task<NotificationChannel> ResolveChannelAsync(NotificationType type, CancellationToken ct)
    {
        if (tenant.IsBypass) return NotificationChannel.Both;

        var t = await db.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == tenant.CurrentTenantId, ct);
        if (t is null) return NotificationChannel.Both;

        var s = t.Notifications;
        return type switch
        {
            NotificationType.LeaveFiled        => s.NewLeaveRequest,
            NotificationType.LeaveApproved     => s.LeaveApproved,
            NotificationType.LeaveRejected     => s.LeaveRejected,
            NotificationType.OvertimeFiled     => s.NewLeaveRequest,    // share OT with leave-request setting
            NotificationType.OvertimeApproved  => s.LeaveApproved,
            NotificationType.OvertimeRejected  => s.LeaveRejected,
            NotificationType.AttendanceFlagged => NotificationChannel.InApp,
            _                                  => NotificationChannel.Both,
        };
    }

    private async Task TrySendEmailAsync(Guid userId, string title, string body, CancellationToken ct)
    {
        var u = await db.Users
            .AsNoTracking()
            .Where(x => x.Id == userId)
            .Select(x => new { x.Email, x.DisplayName })
            .FirstOrDefaultAsync(ct);
        if (u is null) return;
        await TrySendEmailDirectAsync(u.Email, u.DisplayName, title, body, ct);
    }

    private async Task TrySendEmailDirectAsync(string emailAddr, string name, string title, string body, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(emailAddr)) return;
        var html = $"<p>{System.Net.WebUtility.HtmlEncode(body)}</p><p style='color:#888;font-size:12px'>— Giwu HRMS</p>";
        try { await email.SendAsync(emailAddr, name, title, html, ct); }
        catch (Exception ex) { log.LogWarning(ex, "Notification email to {Email} failed: {Title}", emailAddr, title); }
    }
}
