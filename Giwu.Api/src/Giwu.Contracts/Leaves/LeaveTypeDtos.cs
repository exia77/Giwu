using Giwu.Domain.Leaves;

namespace Giwu.Contracts.Leaves;

public sealed record LeaveTypeDto(
    Guid Id,
    string Name,
    string Code,
    LeaveCategory Category,
    string Description,
    bool IsActive,
    bool IsPaid,
    bool IsMandated,
    decimal AnnualEntitlementDays,
    LeaveAccrualMode AccrualMode,
    bool CarryoverAllowed,
    decimal MaxCarryoverDays,
    bool RequiresMedCert,
    int RequiresMedCertAfterDays,
    string LegalReference);

public sealed record CreateLeaveTypeRequest(
    string Name, string Code, LeaveCategory Category, string Description,
    bool IsPaid, bool IsMandated,
    decimal AnnualEntitlementDays, LeaveAccrualMode AccrualMode,
    bool CarryoverAllowed, decimal MaxCarryoverDays,
    bool RequiresMedCert, int RequiresMedCertAfterDays,
    string LegalReference);

public sealed record UpdateLeaveTypeRequest(
    string Name, string Code, LeaveCategory Category, string Description,
    bool IsActive, bool IsPaid, bool IsMandated,
    decimal AnnualEntitlementDays, LeaveAccrualMode AccrualMode,
    bool CarryoverAllowed, decimal MaxCarryoverDays,
    bool RequiresMedCert, int RequiresMedCertAfterDays,
    string LegalReference);

public sealed record LeaveBalanceDto(
    Guid EmployeeId,
    Guid LeaveTypeId,
    string LeaveTypeName,
    string LeaveTypeCode,
    int PeriodYear,
    decimal Entitlement,
    decimal CarryOver,
    decimal Used,
    decimal Pending,
    decimal Available);
