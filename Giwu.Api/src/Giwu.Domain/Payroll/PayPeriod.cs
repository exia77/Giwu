using Giwu.Domain.Common;

namespace Giwu.Domain.Payroll;

public class PayPeriod : AuditableEntity
{
    public string Code { get; set; } = "";
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public DateOnly? ReleaseDate { get; set; }
    public string Frequency { get; set; } = "Monthly";
    public PayPeriodStatus Status { get; set; } = PayPeriodStatus.Draft;
    public Guid? ApprovedById { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public string Notes { get; set; } = "";
}
