using System.Text.Json;
using Giwu.Application.Common;
using Giwu.Domain.Audit;
using Giwu.Domain.Common;
using Giwu.Domain.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Giwu.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Writes a row to <c>audit_events</c> for every Add / Update / Delete on an
/// AuditableEntity. Skips audit/outbox tables themselves to avoid recursion.
/// Properties marked <c>[AuditRedact]</c> are recorded as the literal string
/// <c>"&lt;redacted&gt;"</c> so PII (salary, TIN, SSS#) never lands in the log.
/// Runs in the same transaction as <c>SaveChangesAsync</c>.
/// </summary>
public sealed class AuditInterceptor(ICurrentUser user, TimeProvider clock) : SaveChangesInterceptor
{
    private static readonly HashSet<Type> _skipTypes = new()
    {
        typeof(AuditEvent), typeof(OutboxMessage)
    };

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken ct = default)
    {
        var ctx = eventData.Context;
        if (ctx is null) return base.SavingChangesAsync(eventData, result, ct);

        var entries = ctx.ChangeTracker.Entries<AuditableEntity>()
            .Where(e => !_skipTypes.Contains(e.Entity.GetType())
                     && e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        foreach (var entry in entries)
        {
            var action = entry.State switch
            {
                EntityState.Added    => "Created",
                EntityState.Deleted  => "Deleted",
                _                    => "Updated"
            };

            // For "Modified" entries that are actually soft deletes (DeletedAt was set),
            // record them as Deleted instead.
            if (entry.State == EntityState.Modified
                && entry.Property(nameof(AuditableEntity.DeletedAt)).IsModified
                && entry.Entity.DeletedAt is not null)
            {
                action = "Deleted";
            }

            // Set audit cols explicitly: the DbContext.SaveChangesAsync override
            // that normally sets them has already run by the time this fires.
            ctx.Add(new AuditEvent
            {
                EntityName  = entry.Entity.GetType().Name,
                EntityId    = entry.Entity.Id.ToString(),
                Action      = action,
                ChangesJson = SerializeChanges(entry, action),
                TenantId    = entry.Entity.TenantId,
                CreatedAt   = clock.GetUtcNow(),
                CreatedById = user.IsAuthenticated ? user.Id : Guid.Empty,
            });
        }

        return base.SavingChangesAsync(eventData, result, ct);
    }

    private static string SerializeChanges(EntityEntry<AuditableEntity> entry, string action)
    {
        var redactedProps = entry.Entity.GetType()
            .GetProperties()
            .Where(p => p.GetCustomAttributes(typeof(AuditRedactAttribute), inherit: true).Length > 0)
            .Select(p => p.Name)
            .ToHashSet(StringComparer.Ordinal);

        var diff = new Dictionary<string, object?>();

        foreach (var prop in entry.Properties)
        {
            // Skip noise: audit / concurrency columns
            if (prop.Metadata.Name is nameof(AuditableEntity.CreatedAt)
                                  or nameof(AuditableEntity.CreatedById)
                                  or nameof(AuditableEntity.UpdatedAt)
                                  or nameof(AuditableEntity.UpdatedById)
                                  or nameof(AuditableEntity.Xmin))
                continue;

            var redacted = redactedProps.Contains(prop.Metadata.Name);

            switch (action)
            {
                case "Created":
                    diff[prop.Metadata.Name] = redacted ? "<redacted>" : prop.CurrentValue;
                    break;
                case "Deleted":
                    diff[prop.Metadata.Name] = redacted ? "<redacted>" : prop.OriginalValue;
                    break;
                default: // Updated — only record changed fields
                    if (prop.IsModified && !Equals(prop.OriginalValue, prop.CurrentValue))
                    {
                        diff[prop.Metadata.Name] = redacted
                            ? new { from = "<redacted>", to = "<redacted>" }
                            : (object)new { from = prop.OriginalValue, to = prop.CurrentValue };
                    }
                    break;
            }
        }

        return JsonSerializer.Serialize(diff, JsonOpts);
    }

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };
}
