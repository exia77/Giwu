using Giwu.Application.Common;
using Giwu.Contracts.Notifications;
using Giwu.Domain.Notifications;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Giwu.Application.Notifications;

// ─── Queries ────────────────────────────────────────────────────────────────

public sealed record ListNotificationsQuery(bool UnreadOnly = false, int Limit = 50)
    : IRequest<Result<NotificationSummary>>;

internal sealed class ListNotificationsHandler(IApplicationDbContext db, ICurrentUser user)
    : IRequestHandler<ListNotificationsQuery, Result<NotificationSummary>>
{
    public async Task<Result<NotificationSummary>> Handle(ListNotificationsQuery q, CancellationToken ct)
    {
        if (!user.IsAuthenticated)
            return Result<NotificationSummary>.Forbidden("Not authenticated.");

        var baseQuery = db.Notifications.Where(n => n.RecipientUserId == user.Id);

        var unreadCount = await baseQuery.CountAsync(n => n.ReadAt == null, ct);

        var items = baseQuery;
        if (q.UnreadOnly) items = items.Where(n => n.ReadAt == null);

        var rows = await items
            .OrderByDescending(n => n.CreatedAt)
            .Take(Math.Clamp(q.Limit, 1, 200))
            .Select(n => new NotificationDto(
                n.Id, n.Type, n.Title, n.Body,
                n.RelatedEntityId, n.RelatedEntityType,
                n.ReadAt, n.CreatedAt))
            .ToListAsync(ct);

        return Result<NotificationSummary>.Success(new NotificationSummary(unreadCount, rows));
    }
}

// ─── Mark one ──────────────────────────────────────────────────────────────

public sealed record MarkNotificationReadCommand(Guid Id) : IRequest<Result>;

internal sealed class MarkNotificationReadHandler(
    IApplicationDbContext db, ICurrentUser user, TimeProvider clock)
    : IRequestHandler<MarkNotificationReadCommand, Result>
{
    public async Task<Result> Handle(MarkNotificationReadCommand cmd, CancellationToken ct)
    {
        var n = await db.Notifications.FirstOrDefaultAsync(
            x => x.Id == cmd.Id && x.RecipientUserId == user.Id, ct);
        if (n is null) return Result.NotFound("Notification not found.");
        if (n.ReadAt is null)
        {
            n.ReadAt = clock.GetUtcNow();
            await db.SaveChangesAsync(ct);
        }
        return Result.Success();
    }
}

// ─── Mark all ──────────────────────────────────────────────────────────────

public sealed record MarkAllNotificationsReadCommand : IRequest<Result<int>>;

internal sealed class MarkAllNotificationsReadHandler(
    IApplicationDbContext db, ICurrentUser user, TimeProvider clock)
    : IRequestHandler<MarkAllNotificationsReadCommand, Result<int>>
{
    public async Task<Result<int>> Handle(MarkAllNotificationsReadCommand cmd, CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        var unread = await db.Notifications
            .Where(n => n.RecipientUserId == user.Id && n.ReadAt == null)
            .ToListAsync(ct);
        foreach (var n in unread) n.ReadAt = now;
        await db.SaveChangesAsync(ct);
        return Result<int>.Success(unread.Count);
    }
}
