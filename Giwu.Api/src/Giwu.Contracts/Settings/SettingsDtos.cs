namespace Giwu.Contracts.Settings;

public sealed record OrganizationProfileDto(
    string LegalName,
    string TradeName,
    string Address,
    string City,
    string Province,
    string PostalCode,
    string Phone,
    string Email,
    string Website,
    string Tin,
    string RdoCode,
    string SssEmployerNo,
    string PhilHealthEmployerNo,
    string PagibigEmployerNo,
    string DoleEstablishmentNo);

public sealed record UpdateOrganizationProfileRequest(
    string LegalName,
    string TradeName,
    string Address,
    string City,
    string Province,
    string PostalCode,
    string Phone,
    string Email,
    string Website,
    string Tin,
    string RdoCode,
    string SssEmployerNo,
    string PhilHealthEmployerNo,
    string PagibigEmployerNo,
    string DoleEstablishmentNo);
