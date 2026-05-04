namespace Giwu.Contracts.Reports;

public sealed record HeadcountSummaryDto(
    int TotalEmployees,
    int Active,
    int OnLeave,
    int Suspended,
    int Terminated,
    int Resigned,
    int Departments,
    IReadOnlyList<HeadcountByDepartmentDto> ByDepartment);

public sealed record HeadcountByDepartmentDto(
    Guid DepartmentId,
    string DepartmentName,
    int Count);

public sealed record AttendanceSummaryDto(
    DateOnly From,
    DateOnly To,
    int TotalRecords,
    int Present,
    int Late,
    int Absent,
    int OnLeave,
    decimal TotalHours,
    decimal AverageHoursPerDay);

public sealed record LeaveSummaryDto(
    int TotalRequests,
    int Pending,
    int Approved,
    int Rejected,
    int Cancelled,
    decimal DaysApprovedThisMonth,
    IReadOnlyList<LeaveByTypeDto> ByType);

public sealed record LeaveByTypeDto(
    Guid LeaveTypeId,
    string LeaveTypeName,
    string LeaveTypeCode,
    int RequestCount,
    decimal DaysApproved);

public sealed record PayrollSummaryDto(
    int PayPeriods,
    int Payslips,
    decimal TotalGross,
    decimal TotalNet,
    decimal TotalDeductions,
    IReadOnlyList<PayrollByPeriodDto> ByPeriod);

public sealed record PayrollByPeriodDto(
    Guid PayPeriodId,
    string Code,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    decimal TotalGross,
    decimal TotalNet);
