using Giwu.Application.Common;
using Giwu.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace Giwu.Infrastructure.Billing;

/// <summary>
/// Stripe-backed <see cref="IBillingService"/>. The Checkout flow goes:
///   1. Client clicks Upgrade → POST /api/billing/checkout
///   2. Server here creates/finds a Stripe Customer for the tenant
///   3. Creates a Subscription Checkout Session for the target Price
///   4. Returns the hosted URL → client opens it in the system browser
///   5. User pays → Stripe redirects to SuccessUrl AND fires a webhook
///   6. Webhook handler updates Tenant.SubscriptionTier
/// </summary>
public sealed class StripeBillingService : IBillingService
{
    private readonly StripeOptions _opts;
    private readonly IApplicationDbContext _db;
    private readonly ILogger<StripeBillingService> _log;

    public StripeBillingService(
        IOptions<StripeOptions> opts,
        IApplicationDbContext db,
        ILogger<StripeBillingService> log)
    {
        _opts = opts.Value;
        _db = db;
        _log = log;

        // Stripe.net is keyed off a static config. Setting it once on construction
        // means every SDK call inside this service uses our key.
        if (!string.IsNullOrEmpty(_opts.SecretKey))
            StripeConfiguration.ApiKey = _opts.SecretKey;
    }

    public bool IsConfigured =>
        !string.IsNullOrEmpty(_opts.SecretKey) &&
        !string.IsNullOrEmpty(_opts.Prices.Basic) &&
        !string.IsNullOrEmpty(_opts.Prices.Pro);

    public async Task<string> CreateCheckoutSessionAsync(
        SubscriptionTier targetTier,
        Guid tenantId,
        string tenantName,
        string? tenantEmail,
        CancellationToken ct = default)
    {
        if (!IsConfigured)
            throw new InvalidOperationException(
                "Stripe is not configured. Set Stripe:SecretKey and Stripe:Prices.* in config.");

        var priceId = targetTier switch
        {
            SubscriptionTier.Basic      => _opts.Prices.Basic,
            SubscriptionTier.Pro        => _opts.Prices.Pro,
            SubscriptionTier.Enterprise => _opts.Prices.Enterprise,
            _ => throw new ArgumentOutOfRangeException(nameof(targetTier)),
        };
        if (string.IsNullOrEmpty(priceId))
            throw new InvalidOperationException($"No Stripe price configured for tier {targetTier}.");

        var tenant = await _db.Tenants.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == tenantId, ct)
            ?? throw new InvalidOperationException("Tenant not found");

        // Lazily create a Stripe Customer the first time this tenant transacts.
        // Persist the id on the tenant so subsequent upgrades/portals reuse it.
        if (string.IsNullOrEmpty(tenant.BillingCustomerId))
        {
            var customer = await new CustomerService().CreateAsync(new CustomerCreateOptions
            {
                Name = tenantName,
                Email = tenantEmail,
                Metadata = new Dictionary<string, string> { ["tenant_id"] = tenantId.ToString() },
            }, cancellationToken: ct);
            tenant.BillingCustomerId = customer.Id;
            await _db.SaveChangesAsync(ct);
        }

        var session = await new SessionService().CreateAsync(new SessionCreateOptions
        {
            Mode = "subscription",
            Customer = tenant.BillingCustomerId,
            LineItems = new List<SessionLineItemOptions>
            {
                new() { Price = priceId, Quantity = 1 },
            },
            SuccessUrl = _opts.SuccessUrl,
            CancelUrl  = _opts.CancelUrl,
            ClientReferenceId = tenantId.ToString(),
            Metadata = new Dictionary<string, string>
            {
                ["tenant_id"]    = tenantId.ToString(),
                ["target_tier"]  = targetTier.ToString(),
            },
            // Surfaces the customer's billing address in the receipt; PH tax-receipt
            // requirements often need this.
            BillingAddressCollection = "required",
        }, cancellationToken: ct);

        return session.Url;
    }

    public BillingWebhookEvent? VerifyWebhook(string payload, string stripeSignatureHeader)
    {
        if (string.IsNullOrEmpty(_opts.WebhookSecret))
        {
            _log.LogWarning("Stripe webhook fired but WebhookSecret is not configured — refusing.");
            return null;
        }

        try
        {
            var evt = EventUtility.ConstructEvent(payload, stripeSignatureHeader, _opts.WebhookSecret);

            // Pull a normalized shape out of the (richly typed but verbose) Stripe event.
            string? customerId = null;
            string? priceId = null;
            string? status = null;
            DateTimeOffset? endsAt = null;

            if (evt.Data?.Object is Subscription sub)
            {
                customerId = sub.CustomerId;
                status = sub.Status;
                // Price ID lives on the first subscription item; period_end too.
                // (Stripe.net 48 moved period_end off the subscription itself.)
                var firstItem = sub.Items?.Data?.FirstOrDefault();
                priceId = firstItem?.Price?.Id;
                if (firstItem?.CurrentPeriodEnd is DateTime ipe && ipe > DateTime.MinValue)
                    endsAt = new DateTimeOffset(ipe, TimeSpan.Zero);
            }
            else if (evt.Data?.Object is Invoice inv)
            {
                customerId = inv.CustomerId;
            }
            else if (evt.Data?.Object is Session ses)
            {
                customerId = ses.CustomerId;
            }

            return new BillingWebhookEvent(
                EventId: evt.Id,
                Type: evt.Type,
                CustomerId: customerId,
                PriceId: priceId,
                SubscriptionStatus: status,
                CurrentPeriodEndsAt: endsAt);
        }
        catch (StripeException ex)
        {
            _log.LogWarning(ex, "Stripe webhook signature verification failed.");
            return null;
        }
    }

    public SubscriptionTier? TierForPriceId(string priceId)
    {
        if (string.IsNullOrEmpty(priceId)) return null;
        if (priceId == _opts.Prices.Basic)      return SubscriptionTier.Basic;
        if (priceId == _opts.Prices.Pro)        return SubscriptionTier.Pro;
        if (priceId == _opts.Prices.Enterprise) return SubscriptionTier.Enterprise;
        return null;
    }
}
