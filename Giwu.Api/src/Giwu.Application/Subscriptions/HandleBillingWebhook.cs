using Giwu.Application.Common;
using Giwu.Domain.Tenancy;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Giwu.Application.Subscriptions;

/// <summary>
/// Webhook payload from Stripe. Already verified by the billing service
/// before this command is dispatched.
/// </summary>
public sealed record HandleBillingWebhookCommand(BillingWebhookEvent Event) : IRequest<Result>;

internal sealed class HandleBillingWebhookHandler(
    IApplicationDbContext db,
    IBillingService billing,
    ILogger<HandleBillingWebhookHandler> log)
    : IRequestHandler<HandleBillingWebhookCommand, Result>
{
    public async Task<Result> Handle(HandleBillingWebhookCommand cmd, CancellationToken ct)
    {
        var evt = cmd.Event;
        if (string.IsNullOrEmpty(evt.CustomerId))
        {
            log.LogInformation("Webhook {Type} has no customer id — ignoring.", evt.Type);
            return Result.Success();
        }

        var tenant = await db.Tenants.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.BillingCustomerId == evt.CustomerId, ct);
        if (tenant is null)
        {
            log.LogWarning("Webhook {Type} for customer {Cust} matched no tenant.", evt.Type, evt.CustomerId);
            return Result.Success(); // 200 to Stripe so they stop retrying
        }

        switch (evt.Type)
        {
            // Subscription was created, plan changed, or renewed.
            case "customer.subscription.created":
            case "customer.subscription.updated":
                if (evt.PriceId is not null)
                {
                    var newTier = billing.TierForPriceId(evt.PriceId);
                    if (newTier is SubscriptionTier t) tenant.SubscriptionTier = t;
                }
                tenant.SubscriptionStatus = MapStatus(evt.SubscriptionStatus);
                tenant.CurrentPeriodEndsAt = evt.CurrentPeriodEndsAt;
                break;

            // Customer cancelled. Drop to Basic at end of current period (Stripe
            // keeps the subscription active until period_end), then suspend.
            case "customer.subscription.deleted":
                tenant.SubscriptionStatus = SubscriptionStatus.Cancelled;
                break;

            // Payment failed — flip to PastDue. Grace period before downgrade
            // would normally happen here; we keep them on their current tier
            // and let Stripe's dunning retries play out before downgrading.
            case "invoice.payment_failed":
                tenant.SubscriptionStatus = SubscriptionStatus.PastDue;
                break;

            // Initial checkout completed. The subscription.created event right
            // after this is the source of truth for tier; this one just confirms
            // the customer id link.
            case "checkout.session.completed":
                if (tenant.SubscriptionStatus == SubscriptionStatus.Trial)
                    tenant.SubscriptionStatus = SubscriptionStatus.Active;
                break;

            default:
                log.LogInformation("Unhandled Stripe event type: {Type}", evt.Type);
                return Result.Success();
        }

        await db.SaveChangesAsync(ct);
        log.LogInformation("Processed Stripe event {EventId} ({Type}) for tenant {TenantId} — now {Tier}/{Status}.",
            evt.EventId, evt.Type, tenant.Id, tenant.SubscriptionTier, tenant.SubscriptionStatus);
        return Result.Success();
    }

    private static SubscriptionStatus MapStatus(string? stripeStatus) => stripeStatus switch
    {
        "active"              => SubscriptionStatus.Active,
        "trialing"            => SubscriptionStatus.Trial,
        "past_due"            => SubscriptionStatus.PastDue,
        "canceled"            => SubscriptionStatus.Cancelled,
        "unpaid"              => SubscriptionStatus.PastDue,
        "incomplete"          => SubscriptionStatus.PastDue,
        "incomplete_expired"  => SubscriptionStatus.Cancelled,
        _                     => SubscriptionStatus.Active,
    };
}
