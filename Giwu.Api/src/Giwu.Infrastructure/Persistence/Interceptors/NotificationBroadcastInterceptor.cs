using Giwu.Application.Notifications;
using Giwu.Contracts.Notifications;
using Giwu.Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Giwu.Infrastructure.Persistence.Interceptors;

/// <summary>
/// After a successful SaveChanges, pushes any newly-inserted Notification
/// entities to their recipients via INotificationBroadcaster (SignalR).
/// Captures the snapshot during SavingChangesAsync (entries still flagged
/// Added) and broadcasts in SavedChangesAsync (transaction committed).
/// </summary>
public sealed class NotificationBroadcastInterceptor(INotificationBroadcaster broadcaster)
    : SaveChangesInterceptor
{
    // Per-call buffer keyed on DbContext instance. DbContext is scoped, so
    // concurrent SaveChanges on different scopes don't trample each other.
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<DbContext, List<Notification>> _pending = new();

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken ct = default)
    {
        if (eventData.Context is null) return base.SavingChangesAsync(eventData, result, ct);

        var added = eventData.Context.ChangeTracker
            .Entries<Notification>()
            .Where(e => e.State == EntityState.Added)
            .Select(e => e.Entity)
            .ToList();

        if (added.Count > 0)
            _pending.AddOrUpdate(eventData.Context, added);

        return base.SavingChangesAsync(eventData, result, ct);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken ct = default)
    {
        if (eventData.Context is null) return result;

        if (_pending.TryGetValue(eventData.Context, out var added))
        {
            _pending.Remove(eventData.Context);

            foreach (var n in added)
            {
                var dto = new NotificationDto(
                    n.Id, n.Type, n.Title, n.Body,
                    n.RelatedEntityId, n.RelatedEntityType,
                    n.ReadAt, n.CreatedAt);

                try { await broadcaster.BroadcastAsync(n.RecipientUserId, dto, ct); }
                catch { /* broadcaster failures must not block persistence */ }
            }
        }

        return await base.SavedChangesAsync(eventData, result, ct);
    }
}
