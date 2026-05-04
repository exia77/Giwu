using Giwu.Domain.Attendance;

namespace Giwu.Contracts.Attendance;

public sealed record AttendanceRecordDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeName,
    DateOnly Date,
    DateTimeOffset? ClockIn,
    DateTimeOffset? ClockOut,
    int? BreakMinutes,
    AttendanceStatus Status,
    int? LateMinutes,
    int? UndertimeMinutes,
    int? OvertimeApprovedMinutes,
    string Notes);

public sealed record ClockInRequest(
    string Source = "App",
    string Location = "");

public sealed record ClockOutRequest(
    string Source = "App",
    string Location = "",
    int? BreakMinutes = null);

public sealed record OvertimeRequestDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeName,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    OvertimeType Type,
    string Reason,
    ApprovalStatus Status,
    DateTimeOffset FiledAt);

public sealed record FileOvertimeRequest(
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    OvertimeType Type,
    string Reason);

public sealed record ResolveOvertimeRequest(string Note);
