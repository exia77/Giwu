using Giwu.Domain.Common;

namespace Giwu.Domain.Leaves;

public class LeaveType : AuditableEntity
{
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
    public LeaveCategory Category { get; set; }
    public string Description { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public bool IsPaid { get; set; } = true;
    public bool IsMandated { get; set; }
    public decimal AnnualEntitlementDays { get; set; }
    public LeaveAccrualMode AccrualMode { get; set; }
    public bool CarryoverAllowed { get; set; }
    public decimal MaxCarryoverDays { get; set; }
    public bool RequiresMedCert { get; set; }
    public int RequiresMedCertAfterDays { get; set; } = 2;
    public string LegalReference { get; set; } = "";
}
