using Giwu.Domain.Leaves;

namespace Giwu.Contracts.Leaves;

public sealed record LeaveRequestDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeName,
    Guid LeaveTypeId,
    string LeaveTypeName,
    string LeaveTypeCode,
    DateOnly StartDate,
    DateOnly EndDate,
    bool IsHalfDay,
    decimal DaysRequested,
    string Reason,
    LeaveRequestStatus Status,
    DateTimeOffset FiledAt);

public sealed record FileLeaveRequest(
    Guid LeaveTypeId,
    DateOnly StartDate,
    DateOnly EndDate,
    bool IsHalfDay,
    string HalfDaySession,
    string Reason);

public sealed record ApproveLeaveRequest(string Note);
public sealed record RejectLeaveRequest(string Note);
