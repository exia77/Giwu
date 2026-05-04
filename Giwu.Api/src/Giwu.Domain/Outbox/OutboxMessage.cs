using Giwu.Domain.Common;

namespace Giwu.Domain.Outbox;

/// <summary>
/// Domain events serialized to this table within the same SaveChanges
/// transaction. A Hangfire job dispatches them. Guarantees at-least-once
/// delivery without distributed transactions.
/// </summary>
public class OutboxMessage : AuditableEntity
{
    public string Type { get; set; } = "";          // CLR type FullName
    public string PayloadJson { get; set; } = "";
    public DateTimeOffset? ProcessedAt { get; set; }
    public int Attempts { get; set; }
    public string? LastError { get; set; }
}
