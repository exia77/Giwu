using Giwu.Contracts.Tenancy;
using Giwu.HRMS.Hybrid.Services.Api;

namespace Giwu.HRMS.Hybrid.Services.Tenancy;

public interface ITenancyApi
{
    Task<ApiResult<TenantDto>> GetCurrentAsync(CancellationToken ct = default);
}

public sealed class TenancyApiClient(HttpClient http) : ITenancyApi
{
    public Task<ApiResult<TenantDto>> GetCurrentAsync(CancellationToken ct = default) =>
        ApiClientCore.GetAsync<TenantDto>(http, "/api/tenants/me", ct);
}
