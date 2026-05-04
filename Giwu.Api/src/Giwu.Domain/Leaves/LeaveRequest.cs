using Giwu.Domain.Common;

namespace Giwu.Domain.Leaves;

public class LeaveRequest : AuditableEntity
{
    public Guid EmployeeId { get; set; }
    public Guid LeaveTypeId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public bool IsHalfDay { get; set; }
    public string HalfDaySession { get; set; } = "";
    public decimal DaysRequested { get; set; }
    public string Reason { get; set; } = "";
    public string AttachmentUrl { get; set; } = "";
    public LeaveRequestStatus Status { get; set; } = LeaveRequestStatus.Pending;
    public Guid? ResolvedById { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
    public string ResolutionNote { get; set; } = "";
}

public class LeaveBalance : AuditableEntity
{
    public Guid EmployeeId { get; set; }
    public Guid LeaveTypeId { get; set; }
    public int PeriodYear { get; set; }
    public decimal Entitlement { get; set; }
    public decimal CarryOver { get; set; }
    public decimal Used { get; set; }
    public decimal Pending { get; set; }
    public decimal Available => Entitlement + CarryOver - Used - Pending;
}

public sealed record LeaveRequestApproved(Guid RequestId, Guid EmployeeId, Guid ApproverId)
    : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
