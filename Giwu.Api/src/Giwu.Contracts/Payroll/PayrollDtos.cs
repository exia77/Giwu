using Giwu.Domain.Payroll;

namespace Giwu.Contracts.Payroll;

public sealed record PayPeriodDto(
    Guid Id,
    string Code,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    DateOnly? ReleaseDate,
    string Frequency,
    PayPeriodStatus Status,
    Guid? ApprovedById,
    DateTimeOffset? ApprovedAt,
    string Notes,
    int PayslipCount,
    decimal TotalGross,
    decimal TotalNet);

public sealed record CreatePayPeriodRequest(
    string Code,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    DateOnly? ReleaseDate,
    string Frequency,
    string Notes);

public sealed record UpdatePayPeriodRequest(
    string Code,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    DateOnly? ReleaseDate,
    string Frequency,
    string Notes);

public sealed record PayslipDto(
    Guid Id,
    Guid PayPeriodId,
    Guid EmployeeId,
    string EmployeeName,
    string EmployeeNumber,
    string DepartmentName,
    string JobTitle,
    decimal BasicSalary,
    decimal Overtime,
    decimal Bonus,
    decimal Allowance,
    decimal Sss,
    decimal PhilHealth,
    decimal PagIbig,
    decimal WithholdingTax,
    decimal LoanDeduction,
    decimal OtherDeduction,
    decimal Gross,
    decimal TotalDeductions,
    decimal Net,
    PayslipStatus Status,
    string Notes);

public sealed record CreatePayslipRequest(
    Guid PayPeriodId,
    Guid EmployeeId,
    decimal BasicSalary,
    decimal Overtime,
    decimal Bonus,
    decimal Allowance,
    decimal Sss,
    decimal PhilHealth,
    decimal PagIbig,
    decimal WithholdingTax,
    decimal LoanDeduction,
    decimal OtherDeduction,
    string Notes);

public sealed record UpdatePayslipRequest(
    decimal BasicSalary,
    decimal Overtime,
    decimal Bonus,
    decimal Allowance,
    decimal Sss,
    decimal PhilHealth,
    decimal PagIbig,
    decimal WithholdingTax,
    decimal LoanDeduction,
    decimal OtherDeduction,
    string Notes);

public sealed record ApprovePayPeriodRequest(string Note);
