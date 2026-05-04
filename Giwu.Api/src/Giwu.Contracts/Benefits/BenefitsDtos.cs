using Giwu.Domain.Benefits;

namespace Giwu.Contracts.Benefits;

public sealed record BenefitProgramDto(
    Guid Id,
    string Name,
    string Provider,
    BenefitCategory Category,
    string Description,
    bool IsActive,
    bool IsMandatory,
    string Eligibility,
    DateOnly EffectiveDate,
    DateOnly? RenewalDate,
    decimal MonthlyCostPerEmployee,
    decimal EmployerShare,
    decimal EmployeeShare,
    bool ShareIsPercent,
    int EnrollmentCount);

public sealed record CreateBenefitProgramRequest(
    string Name,
    string Provider,
    BenefitCategory Category,
    string Description,
    bool IsMandatory,
    string Eligibility,
    DateOnly EffectiveDate,
    DateOnly? RenewalDate,
    decimal MonthlyCostPerEmployee,
    decimal EmployerShare,
    decimal EmployeeShare,
    bool ShareIsPercent);

public sealed record UpdateBenefitProgramRequest(
    string Name,
    string Provider,
    BenefitCategory Category,
    string Description,
    bool IsActive,
    bool IsMandatory,
    string Eligibility,
    DateOnly EffectiveDate,
    DateOnly? RenewalDate,
    decimal MonthlyCostPerEmployee,
    decimal EmployerShare,
    decimal EmployeeShare,
    bool ShareIsPercent);

public sealed record BenefitEnrollmentDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeName,
    Guid BenefitProgramId,
    string ProgramName,
    DateOnly EnrolledOn,
    DateOnly? EndDate,
    EnrollmentStatus Status,
    decimal MonthlyContribution,
    string Notes);

public sealed record CreateBenefitEnrollmentRequest(
    Guid EmployeeId,
    Guid BenefitProgramId,
    DateOnly EnrolledOn,
    decimal MonthlyContribution,
    string Notes);

public sealed record UpdateBenefitEnrollmentRequest(
    DateOnly EnrolledOn,
    DateOnly? EndDate,
    EnrollmentStatus Status,
    decimal MonthlyContribution,
    string Notes);

public sealed record BenefitRequestDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeName,
    Guid BenefitProgramId,
    string ProgramName,
    BenefitRequestType Type,
    decimal Amount,
    DateTimeOffset RequestedAt,
    DateTimeOffset? ResolvedAt,
    BenefitRequestStatus Status,
    string Reason,
    int? TermMonths,
    decimal? MonthlyDeduction,
    decimal? OutstandingBalance);

public sealed record FileBenefitRequestRequest(
    Guid EmployeeId,
    Guid BenefitProgramId,
    BenefitRequestType Type,
    decimal Amount,
    string Reason,
    int? TermMonths,
    decimal? MonthlyDeduction);

public sealed record ResolveBenefitRequestRequest(BenefitRequestStatus Status, string Note);
