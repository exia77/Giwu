namespace Giwu.Contracts.Tenancy;

public sealed record TenantDto(
    Guid Id,
    string Name,
    string LegalName,
    string Tin,
    string DefaultCurrency,
    string DefaultTimeZone,
    bool IsActive);
