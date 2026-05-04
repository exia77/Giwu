using Giwu.Domain.Common;

namespace Giwu.Domain.Audit;

public class AuditEvent : AuditableEntity
{
    public string EntityName { get; set; } = "";
    public string EntityId { get; set; } = "";
    public string Action { get; set; } = "";        // Created, Updated, Deleted
    public string ChangesJson { get; set; } = "";   // JSONB { field: { from, to } }
    public string? ActorIp { get; set; }
    public string? UserAgent { get; set; }
}
