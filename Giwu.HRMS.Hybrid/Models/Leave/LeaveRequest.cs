namespace Giwu.HRMS.Hybrid.Models.Leave;

/// <summary>
/// A leave application filed by an employee. Mirrors the row stored in the
/// backend leave_requests table — same shape sent over the API.
/// </summary>
public class LeaveRequest
{
    public string Id { get; set; } = "";
    public string EmployeeId { get; set; } = "";
    public string EmployeeName { get; set; } = "";
    public string EmployeeInit { get; set; } = "";
    public string EmployeeAv { get; set; } = "";
    public string Dept { get; set; } = "";

    public string LeaveTypeId { get; set; } = "";
    public string LeaveTypeName { get; set; } = "";
    public string LeaveTypeCode { get; set; } = "";
    public LeaveCategory LeaveCategory { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsHalfDay { get; set; }
    public string HalfDaySession { get; set; } = ""; // "AM" or "PM"
    public decimal DaysRequested { get; set; }       // computed at file time, working days

    public string Reason { get; set; } = "";
    public string? AttachmentName { get; set; }      // medical cert, etc.

    public DateTime FiledOn { get; set; }
    public LeaveRequestStatus Status { get; set; }
    public DateTime? ResolvedOn { get; set; }
    public string? ResolvedBy { get; set; }
    public string? ResolutionNote { get; set; }
}
