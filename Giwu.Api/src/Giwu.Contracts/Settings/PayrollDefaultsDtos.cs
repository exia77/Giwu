using Giwu.Domain.Tenancy;

namespace Giwu.Contracts.Settings;

public sealed record PayrollDefaultsDto(
    PayFrequency PayFrequency,
    int FirstCutoffDay,
    int SecondCutoffDay,
    decimal RegularOvertimeRate,
    decimal RestDayOvertimeRate,
    decimal HolidayOvertimeRate,
    decimal NightDifferentialRate,
    bool IncludeAllowanceIn13thMonth,
    bool IncludeOtIn13thMonth,
    bool RoundStatutoryDeductions);

public sealed record UpdatePayrollDefaultsRequest(
    PayFrequency PayFrequency,
    int FirstCutoffDay,
    int SecondCutoffDay,
    decimal RegularOvertimeRate,
    decimal RestDayOvertimeRate,
    decimal HolidayOvertimeRate,
    decimal NightDifferentialRate,
    bool IncludeAllowanceIn13thMonth,
    bool IncludeOtIn13thMonth,
    bool RoundStatutoryDeductions);
