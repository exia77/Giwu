using Giwu.Domain.Common;

namespace Giwu.Domain.Tenancy;

public class Tenant : AuditableEntity
{
    public string Name { get; set; } = "";
    public string LegalName { get; set; } = "";
    public string Tin { get; set; } = "";
    public string SssEmployerNumber { get; set; } = "";
    public string PhilHealthEmployerNumber { get; set; } = "";
    public string PagibigEmployerNumber { get; set; } = "";
    public string DefaultCurrency { get; set; } = "PHP";
    public string DefaultTimeZone { get; set; } = "Asia/Manila";
    public bool IsActive { get; set; } = true;
}
