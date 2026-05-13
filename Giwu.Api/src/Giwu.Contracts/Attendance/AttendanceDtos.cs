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

/// <summary>HR-admin edit of an existing attendance record. Times are
/// transmitted as TimeOnly?; the server combines them with the record's
/// stored Date to produce DateTimeOffset. Null clears the value.</summary>
public sealed record UpdateAttendanceRequest(
    AttendanceStatus Status,
    TimeOnly? ClockInTime,
    TimeOnly? ClockOutTime,
    string Notes);

/// <summary>HR-admin manual creation of an attendance entry on behalf of an
/// employee. If a record already exists for (EmployeeId, Date), it is
/// updated instead. Useful for back-filling missed clock-ins.</summary>
public sealed record ManualAttendanceEntryRequest(
    Guid EmployeeId,
    DateOnly Date,
    AttendanceStatus Status,
    TimeOnly? ClockInTime,
    TimeOnly? ClockOutTime,
    string Notes);

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
