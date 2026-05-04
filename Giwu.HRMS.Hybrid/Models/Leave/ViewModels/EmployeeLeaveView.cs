namespace Giwu.HRMS.Hybrid.Models.Leave.ViewModels;

/// <summary>
/// UI-only aggregate that combines an employee profile with their balances and
/// requests. The API doesn't return this exact shape — pages compose it from
/// <see cref="LeaveBalance"/> and <see cref="LeaveRequest"/> for the dialog views.
/// </summary>
public class EmployeeLeaveView
{
    public string EmployeeId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Init { get; set; } = "";
    public string Av { get; set; } = "";
    public string Role { get; set; } = "";
    public string Dept { get; set; } = "";
    public DateTime HireDate { get; set; }
    public string Gender { get; set; } = "";        // affects which mandated types are visible

    public List<LeaveBalance> Balances { get; set; } = new();
    public List<LeaveRequest> Requests { get; set; } = new();
}
