using Giwu.Contracts.Settings;
using Giwu.HRMS.Hybrid.Services.Api;

namespace Giwu.HRMS.Hybrid.Services.Settings;

public interface ISettingsApi
{
    // Branding
    Task<ApiResult<BrandingSettingsDto>> GetBrandingAsync(CancellationToken ct = default);
    Task<ApiResult> UpdateBrandingAsync(UpdateBrandingSettingsRequest body, CancellationToken ct = default);

    // Reset to defaults
    Task<ApiResult> ResetToDefaultsAsync(CancellationToken ct = default);

    // Organization
    Task<ApiResult<OrganizationProfileDto>> GetOrganizationAsync(CancellationToken ct = default);
    Task<ApiResult> UpdateOrganizationAsync(UpdateOrganizationProfileRequest body, CancellationToken ct = default);

    // Localization
    Task<ApiResult<LocalizationSettingsDto>> GetLocalizationAsync(CancellationToken ct = default);
    Task<ApiResult> UpdateLocalizationAsync(UpdateLocalizationSettingsRequest body, CancellationToken ct = default);

    // Payroll defaults
    Task<ApiResult<PayrollDefaultsDto>> GetPayrollAsync(CancellationToken ct = default);
    Task<ApiResult> UpdatePayrollAsync(UpdatePayrollDefaultsRequest body, CancellationToken ct = default);

    // Notifications
    Task<ApiResult<NotificationSettingsDto>> GetNotificationsAsync(CancellationToken ct = default);
    Task<ApiResult> UpdateNotificationsAsync(UpdateNotificationSettingsRequest body, CancellationToken ct = default);

    // Security
    Task<ApiResult<SecuritySettingsDto>> GetSecurityAsync(CancellationToken ct = default);
    Task<ApiResult> UpdateSecurityAsync(UpdateSecuritySettingsRequest body, CancellationToken ct = default);
}

public sealed class SettingsApiClient(HttpClient http) : ISettingsApi
{
    public Task<ApiResult<BrandingSettingsDto>> GetBrandingAsync(CancellationToken ct = default) =>
        ApiClientCore.GetAsync<BrandingSettingsDto>(http, "/api/settings/branding", ct);

    public Task<ApiResult> UpdateBrandingAsync(UpdateBrandingSettingsRequest body, CancellationToken ct = default) =>
        ApiClientCore.PutAsync(http, "/api/settings/branding", body, ct);

    public Task<ApiResult> ResetToDefaultsAsync(CancellationToken ct = default) =>
        ApiClientCore.PostEmptyAsync(http, "/api/settings/reset", ct);

    public Task<ApiResult<OrganizationProfileDto>> GetOrganizationAsync(CancellationToken ct = default) =>
        ApiClientCore.GetAsync<OrganizationProfileDto>(http, "/api/settings/organization", ct);

    public Task<ApiResult> UpdateOrganizationAsync(UpdateOrganizationProfileRequest body, CancellationToken ct = default) =>
        ApiClientCore.PutAsync(http, "/api/settings/organization", body, ct);

    public Task<ApiResult<LocalizationSettingsDto>> GetLocalizationAsync(CancellationToken ct = default) =>
        ApiClientCore.GetAsync<LocalizationSettingsDto>(http, "/api/settings/localization", ct);

    public Task<ApiResult> UpdateLocalizationAsync(UpdateLocalizationSettingsRequest body, CancellationToken ct = default) =>
        ApiClientCore.PutAsync(http, "/api/settings/localization", body, ct);

    public Task<ApiResult<PayrollDefaultsDto>> GetPayrollAsync(CancellationToken ct = default) =>
        ApiClientCore.GetAsync<PayrollDefaultsDto>(http, "/api/settings/payroll", ct);

    public Task<ApiResult> UpdatePayrollAsync(UpdatePayrollDefaultsRequest body, CancellationToken ct = default) =>
        ApiClientCore.PutAsync(http, "/api/settings/payroll", body, ct);

    public Task<ApiResult<NotificationSettingsDto>> GetNotificationsAsync(CancellationToken ct = default) =>
        ApiClientCore.GetAsync<NotificationSettingsDto>(http, "/api/settings/notifications", ct);

    public Task<ApiResult> UpdateNotificationsAsync(UpdateNotificationSettingsRequest body, CancellationToken ct = default) =>
        ApiClientCore.PutAsync(http, "/api/settings/notifications", body, ct);

    public Task<ApiResult<SecuritySettingsDto>> GetSecurityAsync(CancellationToken ct = default) =>
        ApiClientCore.GetAsync<SecuritySettingsDto>(http, "/api/settings/security", ct);

    public Task<ApiResult> UpdateSecurityAsync(UpdateSecuritySettingsRequest body, CancellationToken ct = default) =>
        ApiClientCore.PutAsync(http, "/api/settings/security", body, ct);
}
