using Giwu.Domain.Tenancy;

namespace Giwu.Contracts.Settings;

public sealed record LocalizationSettingsDto(
    string Timezone,
    DateFormat DateFormat,
    string CurrencyCode,
    string CurrencySymbol,
    WeekStart WeekStart,
    int FiscalYearStartMonth);

public sealed record UpdateLocalizationSettingsRequest(
    string Timezone,
    DateFormat DateFormat,
    string CurrencyCode,
    string CurrencySymbol,
    WeekStart WeekStart,
    int FiscalYearStartMonth);
