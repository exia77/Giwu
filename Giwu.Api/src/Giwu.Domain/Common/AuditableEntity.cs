using MediatR;

namespace Giwu.Domain.Common;

public abstract class AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid CreatedById { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedById { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedById { get; set; }
    public uint Xmin { get; set; } // Postgres system column for optimistic concurrency

    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    public void Raise(IDomainEvent ev) => _domainEvents.Add(ev);
    public void ClearDomainEvents() => _domainEvents.Clear();
}

/// <summary>
/// Marker for events raised by aggregates. Inherits MediatR's INotification so
/// the outbox dispatcher can publish them directly to handlers.
/// </summary>
public interface IDomainEvent : INotification
{
    DateTimeOffset OccurredAt { get; }
}

/// <summary>
/// Mark a property to skip its value in the audit log (e.g. salary, TIN).
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class AuditRedactAttribute : Attribute;
