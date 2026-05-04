using Giwu.Domain.Common;

namespace Giwu.Domain.Attendance;

public class AttendanceRecord : AuditableEntity
{
    public Guid EmployeeId { get; set; }
    public DateOnly Date { get; set; }
    public DateTimeOffset? ClockIn { get; set; }
    public DateTimeOffset? ClockOut { get; set; }
    public int? BreakMinutes { get; set; }
    public AttendanceSource ClockInSource { get; set; }
    public AttendanceSource ClockOutSource { get; set; }
    public string ClockInLocation { get; set; } = "";
    public string ClockOutLocation { get; set; } = "";
    public AttendanceStatus Status { get; set; }
    public int? LateMinutes { get; set; }
    public int? UndertimeMinutes { get; set; }
    public int? OvertimeApprovedMinutes { get; set; }
    public string Notes { get; set; } = "";
}

public class OvertimeRequest : AuditableEntity
{
    public Guid EmployeeId { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public OvertimeType Type { get; set; }
    public string Reason { get; set; } = "";
    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;
    public Guid? ApprovedById { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public string ResolutionNote { get; set; } = "";
}
