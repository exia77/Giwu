namespace Giwu.HRMS.Hybrid.Models.Leave;

/// <summary>
/// Per-employee, per-leave-type running balance for the current period.
/// </summary>
public class LeaveBalance
{
    public string EmployeeId { get; set; } = "";
    public string LeaveTypeId { get; set; } = "";
    public decimal Entitlement { get; set; }         // total available this period
    public decimal CarryOver { get; set; }
    public decimal Used { get; set; }
    public decimal Pending { get; set; }             // tied up in pending requests
    public decimal Available => Entitlement + CarryOver - Used - Pending;
    public DateTime? LastTakenOn { get; set; }
}
