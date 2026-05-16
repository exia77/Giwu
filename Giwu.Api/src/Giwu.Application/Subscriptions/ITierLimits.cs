using Giwu.Application.Common;
using Giwu.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Giwu.Application.Subscriptions;

/// <summary>
/// Single source of truth for "what is each tier allowed to do?".
/// Add a new gate by giving it a Check* method here; handlers consult this
/// service instead of hard-coding caps inline.
/// </summary>
public interface ITierLimits
{
    /// <summary>Max number of active employees a tier supports. null = unlimited.</summary>
    int? EmployeeLimit(SubscriptionTier tier);

    /// <summary>Public label for the tier, used in upgrade messages.</summary>
    string Label(SubscriptionTier tier);

    /// <summary>
    /// Loads the current tenant's tier and answers whether one more employee
    /// can be added. Returns <see cref="Result"/>.Success on allowed, or
    /// PaymentRequired with a user-facing message that names the limit and the
    /// next tier up.
    /// </summary>
    Task<Result> CheckEmployeeCreationAsync(CancellationToken ct);
}

public sealed class TierLimits : ITierLimits
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantContext _tenant;

    public TierLimits(IApplicationDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public int? EmployeeLimit(SubscriptionTier tier) => tier switch
    {
        SubscriptionTier.Basic      => 10,
        SubscriptionTier.Pro        => 50,
        SubscriptionTier.Enterprise => null,
        _ => 10,
    };

    public string Label(SubscriptionTier tier) => tier switch
    {
        SubscriptionTier.Basic      => "Basic",
        SubscriptionTier.Pro        => "Pro",
        SubscriptionTier.Enterprise => "Enterprise",
        _ => tier.ToString(),
    };

    public async Task<Result> CheckEmployeeCreationAsync(CancellationToken ct)
    {
        // The Tenant row is excluded from the tenant filter (it IS the tenant),
        // so we can read it directly with IgnoreQueryFilters.
        var tenantRow = await FindCurrentTenantAsync(ct);
        if (tenantRow is null) return Result.Success(); // never gate when tenant context isn't resolved

        var limit = EmployeeLimit(tenantRow.SubscriptionTier);
        if (limit is null) return Result.Success(); // Enterprise — unlimited

        // Count current employees in this tenant. The tenant filter on Employees
        // already scopes this to the current tenant; soft-deleted rows are skipped
        // by the filter too, so this is "active rows in our tenant".
        var current = await _db.Employees.CountAsync(ct);

        if (current < limit.Value) return Result.Success();

        return Result.PaymentRequired(
            $"You've reached the {Label(tenantRow.SubscriptionTier)} plan's {limit.Value}-employee limit. "
          + "Upgrade your plan in Settings → Plan & Billing to add more.");
    }

    private async Task<Tenant?> FindCurrentTenantAsync(CancellationToken ct)
    {
        if (_tenant.CurrentTenantId == Guid.Empty) return null;
        return await _db.Tenants.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == _tenant.CurrentTenantId, ct);
    }
}
