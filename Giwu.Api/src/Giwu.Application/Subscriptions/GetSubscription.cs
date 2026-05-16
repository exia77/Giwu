using Giwu.Application.Common;
using Giwu.Contracts.Tenancy;
using Giwu.Domain.Tenancy;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Giwu.Application.Subscriptions;

public sealed record GetSubscriptionQuery() : IRequest<Result<SubscriptionDto>>;

internal sealed class GetSubscriptionHandler(
    IApplicationDbContext db,
    ITenantContext tenant,
    ITierLimits tiers)
    : IRequestHandler<GetSubscriptionQuery, Result<SubscriptionDto>>
{
    public async Task<Result<SubscriptionDto>> Handle(GetSubscriptionQuery q, CancellationToken ct)
    {
        if (tenant.CurrentTenantId == Guid.Empty)
            return Result<SubscriptionDto>.NotFound();

        var t = await db.Tenants.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == tenant.CurrentTenantId, ct);
        if (t is null) return Result<SubscriptionDto>.NotFound();

        var count = await db.Employees.CountAsync(ct);

        return Result<SubscriptionDto>.Success(new SubscriptionDto(
            Tier:               t.SubscriptionTier,
            Status:             t.SubscriptionStatus,
            TierLabel:          tiers.Label(t.SubscriptionTier),
            EmployeeLimit:      tiers.EmployeeLimit(t.SubscriptionTier),
            EmployeeCount:      count,
            CurrentPeriodEndsAt: t.CurrentPeriodEndsAt,
            TrialEndsAt:        t.TrialEndsAt));
    }
}

public sealed record ChangeSubscriptionTierCommand(SubscriptionTier Tier)
    : IRequest<Result<SubscriptionDto>>;

internal sealed class ChangeSubscriptionTierHandler(
    IApplicationDbContext db,
    ITenantContext tenant,
    IMediator mediator)
    : IRequestHandler<ChangeSubscriptionTierCommand, Result<SubscriptionDto>>
{
    public async Task<Result<SubscriptionDto>> Handle(ChangeSubscriptionTierCommand cmd, CancellationToken ct)
    {
        if (tenant.CurrentTenantId == Guid.Empty)
            return Result<SubscriptionDto>.NotFound();

        var t = await db.Tenants.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == tenant.CurrentTenantId, ct);
        if (t is null) return Result<SubscriptionDto>.NotFound();

        t.SubscriptionTier = cmd.Tier;
        // Manual switch keeps the existing status — when this is hooked to Stripe,
        // the webhook handler will be the one moving Trial → Active etc.
        await db.SaveChangesAsync(ct);

        return await mediator.Send(new GetSubscriptionQuery(), ct);
    }
}
