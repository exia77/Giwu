using Giwu.Contracts.Tenancy;
using Giwu.Domain.Tenancy;
using Giwu.HRMS.Hybrid.Services.Api;

namespace Giwu.HRMS.Hybrid.Services.Tenancy;

public interface ITenancyApi
{
    Task<ApiResult<TenantDto>> GetCurrentAsync(CancellationToken ct = default);
    Task<ApiResult<SubscriptionDto>> GetSubscriptionAsync(CancellationToken ct = default);
    Task<ApiResult<SubscriptionDto>> ChangeTierAsync(SubscriptionTier tier, CancellationToken ct = default);
    Task<ApiResult<CheckoutSessionDto>> CreateCheckoutSessionAsync(SubscriptionTier tier, CancellationToken ct = default);
}

public sealed class TenancyApiClient(HttpClient http) : ITenancyApi
{
    public Task<ApiResult<TenantDto>> GetCurrentAsync(CancellationToken ct = default) =>
        ApiClientCore.GetAsync<TenantDto>(http, "/api/tenants/me", ct);

    public Task<ApiResult<SubscriptionDto>> GetSubscriptionAsync(CancellationToken ct = default) =>
        ApiClientCore.GetAsync<SubscriptionDto>(http, "/api/tenants/me/subscription", ct);

    public Task<ApiResult<SubscriptionDto>> ChangeTierAsync(SubscriptionTier tier, CancellationToken ct = default) =>
        ApiClientCore.PostAsync<ChangeSubscriptionTierRequest, SubscriptionDto>(
            http, "/api/tenants/me/subscription", new ChangeSubscriptionTierRequest(tier), ct);

    public Task<ApiResult<CheckoutSessionDto>> CreateCheckoutSessionAsync(SubscriptionTier tier, CancellationToken ct = default) =>
        ApiClientCore.PostAsync<CreateCheckoutSessionRequest, CheckoutSessionDto>(
            http, "/api/billing/checkout", new CreateCheckoutSessionRequest(tier), ct);
}
