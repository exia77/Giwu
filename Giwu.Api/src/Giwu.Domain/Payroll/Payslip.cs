using Giwu.Domain.Common;

namespace Giwu.Domain.Payroll;

public class Payslip : AuditableEntity
{
    public Guid PayPeriodId { get; set; }
    public Guid EmployeeId { get; set; }

    public decimal BasicSalary { get; set; }
    public decimal Overtime { get; set; }
    public decimal Bonus { get; set; }
    public decimal Allowance { get; set; }

    [AuditRedact] public decimal Sss { get; set; }
    [AuditRedact] public decimal PhilHealth { get; set; }
    [AuditRedact] public decimal PagIbig { get; set; }
    [AuditRedact] public decimal WithholdingTax { get; set; }
    [AuditRedact] public decimal LoanDeduction { get; set; }
    [AuditRedact] public decimal OtherDeduction { get; set; }

    public PayslipStatus Status { get; set; } = PayslipStatus.Pending;
    public string Notes { get; set; } = "";

    public decimal Gross => BasicSalary + Overtime + Bonus + Allowance;
    public decimal TotalDeductions => Sss + PhilHealth + PagIbig + WithholdingTax + LoanDeduction + OtherDeduction;
    public decimal Net => Gross - TotalDeductions;
}
