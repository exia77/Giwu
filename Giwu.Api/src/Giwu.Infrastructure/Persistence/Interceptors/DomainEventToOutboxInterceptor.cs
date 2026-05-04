using System.Text.Json;
using Giwu.Domain.Common;
using Giwu.Domain.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Giwu.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Walks tracked aggregates, copies their <see cref="AuditableEntity.DomainEvents"/>
/// into the <c>outbox_messages</c> table, and clears the in-memory list. Runs in
/// the same transaction as <c>SaveChangesAsync</c>, so the outbox row commits
/// atomically with the state change that produced it. A Hangfire job picks up
/// pending rows and dispatches them to MediatR notification handlers.
/// </summary>
public sealed class DomainEventToOutboxInterceptor(TimeProvider clock) : SaveChangesInterceptor
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken ct = default)
    {
        var ctx = eventData.Context;
        if (ctx is null) return base.SavingChangesAsync(eventData, result, ct);

        var roots = ctx.ChangeTracker.Entries<AuditableEntity>()
            .Select(e => e.Entity)
            .Where(e => e.DomainEvents.Count > 0)
            .ToList();

        foreach (var root in roots)
        {
            foreach (var ev in root.DomainEvents)
            {
                var typeName = ev.GetType().AssemblyQualifiedName
                    ?? throw new InvalidOperationException("Domain event type has no qualified name.");

                ctx.Add(new OutboxMessage
                {
                    Type        = typeName,
                    PayloadJson = JsonSerializer.Serialize(ev, ev.GetType(), JsonOpts),
                    TenantId    = root.TenantId,
                    CreatedAt   = clock.GetUtcNow(),
                });
            }
            root.ClearDomainEvents();
        }

        return base.SavingChangesAsync(eventData, result, ct);
    }
}
