using Giwu.Domain.Common;

namespace Giwu.Domain.Tenancy;

public class Tenant : AuditableEntity
{
    public string Name { get; set; } = "";
    public string LegalName { get; set; } = "";
    public string TradeName { get; set; } = "";

    public string Address { get; set; } = "";
    public string City { get; set; } = "";
    public string Province { get; set; } = "";
    public string PostalCode { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Email { get; set; } = "";
    public string Website { get; set; } = "";

    public string Tin { get; set; } = "";
    public string RdoCode { get; set; } = "";
    public string SssEmployerNumber { get; set; } = "";
    public string PhilHealthEmployerNumber { get; set; } = "";
    public string PagibigEmployerNumber { get; set; } = "";
    public string DoleEstablishmentNumber { get; set; } = "";

    public string DefaultCurrency { get; set; } = "PHP";
    public string DefaultTimeZone { get; set; } = "Asia/Manila";
    public bool IsActive { get; set; } = true;

    public BrandingSettings      Branding      { get; set; } = new();
    public LocalizationSettings  Localization  { get; set; } = new();
    public PayrollDefaults       Payroll       { get; set; } = new();
    public NotificationSettings  Notifications { get; set; } = new();
    public SecuritySettings      Security      { get; set; } = new();
}
