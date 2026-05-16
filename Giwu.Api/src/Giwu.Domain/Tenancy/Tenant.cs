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

    // ── Subscription / billing ─────────────────────────────────────────────
    /// <summary>Current paid tier. Defaults to Basic for new tenants.</summary>
    public SubscriptionTier SubscriptionTier { get; set; } = SubscriptionTier.Basic;

    /// <summary>Lifecycle state — Trial / Active / PastDue / etc.</summary>
    public SubscriptionStatus SubscriptionStatus { get; set; } = SubscriptionStatus.Active;

    /// <summary>Stripe (or future provider) customer id. Null until first checkout.</summary>
    public string? BillingCustomerId { get; set; }

    /// <summary>When the current billing period ends. Null for tenants without a billing relationship yet.</summary>
    public DateTimeOffset? CurrentPeriodEndsAt { get; set; }

    /// <summary>When the trial expires. Null if not in trial.</summary>
    public DateTimeOffset? TrialEndsAt { get; set; }
}
