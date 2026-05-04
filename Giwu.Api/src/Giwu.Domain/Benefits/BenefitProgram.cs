using Giwu.Domain.Common;

namespace Giwu.Domain.Benefits;

public class BenefitProgram : AuditableEntity
{
    public string Name { get; set; } = "";
    public string Provider { get; set; } = "";
    public BenefitCategory Category { get; set; }
    public string Description { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public bool IsMandatory { get; set; }
    public string Eligibility { get; set; } = "";
    public DateOnly EffectiveDate { get; set; }
    public DateOnly? RenewalDate { get; set; }
    public decimal MonthlyCostPerEmployee { get; set; }
    public decimal EmployerShare { get; set; }
    public decimal EmployeeShare { get; set; }
    public bool ShareIsPercent { get; set; }
}
