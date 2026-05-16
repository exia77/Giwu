using Giwu.Domain.Tenancy;

namespace Giwu.Contracts.Tenancy;

/// <summary>
/// Snapshot of the current tenant's billing/tier state. Returned by
/// <c>GET /api/tenancy/subscription</c>. Used by the Settings → Plan &amp; Billing
/// page and by the "you've hit the limit" upgrade prompts.
/// </summary>
public sealed record SubscriptionDto(
    SubscriptionTier   Tier,
    SubscriptionStatus Status,
    string             TierLabel,
    int?               EmployeeLimit,
    int                EmployeeCount,
    DateTimeOffset?    CurrentPeriodEndsAt,
    DateTimeOffset?    TrialEndsAt);

/// <summary>
/// Manual tier change. In production this would be driven by a payment
/// webhook; the demo exposes it as a plain endpoint so HR Admin can switch
/// plans from the Settings UI for testing.
/// </summary>
public sealed record ChangeSubscriptionTierRequest(SubscriptionTier Tier);

/// <summary>
/// Body for POST /api/billing/checkout — picks which tier to upgrade/downgrade to.
/// </summary>
public sealed record CreateCheckoutSessionRequest(SubscriptionTier Tier);

/// <summary>
/// Response: the Stripe-hosted Checkout URL the client should open.
/// </summary>
public sealed record CheckoutSessionDto(string Url);
