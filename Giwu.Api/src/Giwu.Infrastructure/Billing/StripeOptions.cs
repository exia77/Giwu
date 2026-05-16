namespace Giwu.Infrastructure.Billing;

/// <summary>
/// Stripe credentials and tier → Price ID mapping. Configured via
/// appsettings or env vars (Stripe__SecretKey, Stripe__WebhookSecret, etc).
/// Leaving SecretKey empty disables billing — checkout endpoint returns 503.
/// </summary>
public sealed class StripeOptions
{
    /// <summary>sk_live_… or sk_test_… — server-side only, never expose.</summary>
    public string SecretKey { get; set; } = "";

    /// <summary>whsec_… from the Stripe Dashboard → Developers → Webhooks page.</summary>
    public string WebhookSecret { get; set; } = "";

    /// <summary>Stripe Price IDs, one per tier. Format: price_…</summary>
    public StripePrices Prices { get; set; } = new();

    /// <summary>URL Stripe redirects to after a successful checkout. The Hybrid
    /// app intercepts this URL and refreshes the subscription card.</summary>
    public string SuccessUrl { get; set; } = "https://giwu-hrms/local/billing/success";

    /// <summary>URL Stripe redirects to if the user clicks Cancel.</summary>
    public string CancelUrl  { get; set; } = "https://giwu-hrms/local/billing/cancel";
}

public sealed class StripePrices
{
    public string Basic      { get; set; } = "";
    public string Pro        { get; set; } = "";
    public string Enterprise { get; set; } = "";
}
