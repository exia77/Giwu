using System.Text.Json;
using Giwu.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Giwu.Api.Jobs;

/// <summary>
/// Polls <c>outbox_messages</c>, deserializes each row to its concrete domain
/// event type, and publishes it via MediatR. Hangfire schedules this every
/// minute. Runs in bypass mode so it spans all tenants.
/// </summary>
public sealed class OutboxDispatcherJob(
    IApplicationDbContext db,
    IPublisher publisher,
    ITenantContext tenant,
    TimeProvider clock,
    ILogger<OutboxDispatcherJob> log)
{
    private const int BatchSize = 50;
    private const int MaxAttempts = 5;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public async Task RunAsync(CancellationToken ct = default)
    {
        tenant.Bypass();

        var pending = await db.Outbox
            .Where(m => m.ProcessedAt == null && m.Attempts < MaxAttempts)
            .OrderBy(m => m.CreatedAt)
            .Take(BatchSize)
            .ToListAsync(ct);

        if (pending.Count == 0) return;

        var dispatched = 0;
        foreach (var msg in pending)
        {
            try
            {
                var type = Type.GetType(msg.Type, throwOnError: false)
                    ?? throw new InvalidOperationException($"Cannot resolve type {msg.Type}");

                var deserialized = JsonSerializer.Deserialize(msg.PayloadJson, type, JsonOpts)
                    ?? throw new InvalidOperationException("Deserialized payload was null");

                if (deserialized is not INotification notification)
                    throw new InvalidOperationException(
                        $"Type {msg.Type} does not implement INotification");

                // Restore the original tenant so handlers run in the right scope
                tenant.SetTenant(msg.TenantId);

                await publisher.Publish(notification, ct);

                msg.ProcessedAt = clock.GetUtcNow();
                msg.LastError   = null;
                dispatched++;
            }
            catch (Exception ex)
            {
                msg.Attempts++;
                msg.LastError = ex.Message.Length > 1000 ? ex.Message[..1000] : ex.Message;
                log.LogWarning(ex, "Outbox dispatch failed for {Id} (attempt {Attempts})",
                    msg.Id, msg.Attempts);
            }
            finally
            {
                tenant.Bypass(); // back to bypass for the next iteration
            }
        }

        await db.SaveChangesAsync(ct);
        log.LogInformation("Outbox: dispatched {Done}/{Total}", dispatched, pending.Count);
    }
}

public sealed class AttendanceRollupJob(
    ITenantContext tenant,
    ILogger<AttendanceRollupJob> log)
{
    public Task RunAsync(CancellationToken ct = default)
    {
        tenant.Bypass();
        // TODO: scan yesterday's attendance rows, mark Late / Absent / Undertime,
        // post Outbox notifications for managers when thresholds are crossed.
        log.LogInformation("Attendance rollup placeholder ran at {Now}", DateTimeOffset.UtcNow);
        return Task.CompletedTask;
    }
}
