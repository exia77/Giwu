using Giwu.Application.Common;
using Giwu.Contracts.Tenancy;
using Giwu.Domain.Tenancy;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Giwu.Application.Subscriptions;

public sealed record CreateCheckoutSessionCommand(SubscriptionTier TargetTier)
    : IRequest<Result<CheckoutSessionDto>>;

internal sealed class CreateCheckoutSessionHandler(
    IApplicationDbContext db,
    ITenantContext tenant,
    IBillingService billing)
    : IRequestHandler<CreateCheckoutSessionCommand, Result<CheckoutSessionDto>>
{
    public async Task<Result<CheckoutSessionDto>> Handle(CreateCheckoutSessionCommand cmd, CancellationToken ct)
    {
        if (!billing.IsConfigured)
            return Result<CheckoutSessionDto>.Forbidden(
                "Billing is not configured on this server. Contact your administrator to set up Stripe.");

        if (tenant.CurrentTenantId == Guid.Empty)
            return Result<CheckoutSessionDto>.NotFound();

        var t = await db.Tenants.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == tenant.CurrentTenantId, ct);
        if (t is null) return Result<CheckoutSessionDto>.NotFound();

        var url = await billing.CreateCheckoutSessionAsync(
            cmd.TargetTier,
            t.Id,
            t.Name,
            string.IsNullOrWhiteSpace(t.Email) ? null : t.Email,
            ct);

        return Result<CheckoutSessionDto>.Success(new CheckoutSessionDto(url));
    }
}
