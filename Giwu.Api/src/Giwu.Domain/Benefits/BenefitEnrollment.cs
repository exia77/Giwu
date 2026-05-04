using Giwu.Domain.Common;

namespace Giwu.Domain.Benefits;

public class BenefitEnrollment : AuditableEntity
{
    public Guid EmployeeId { get; set; }
    public Guid BenefitProgramId { get; set; }
    public DateOnly EnrolledOn { get; set; }
    public DateOnly? EndDate { get; set; }
    public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Active;
    public decimal MonthlyContribution { get; set; }
    public string Notes { get; set; } = "";
}

public class BenefitRequest : AuditableEntity
{
    public Guid EmployeeId { get; set; }
    public Guid BenefitProgramId { get; set; }
    public BenefitRequestType Type { get; set; }
    public decimal Amount { get; set; }
    public DateTimeOffset RequestedAt { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
    public BenefitRequestStatus Status { get; set; } = BenefitRequestStatus.Pending;
    public string Reason { get; set; } = "";
    public int? TermMonths { get; set; }
    public decimal? MonthlyDeduction { get; set; }
    public decimal? OutstandingBalance { get; set; }
}
