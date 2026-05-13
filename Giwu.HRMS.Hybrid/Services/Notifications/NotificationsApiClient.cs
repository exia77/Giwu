using Giwu.Contracts.Notifications;
using Giwu.HRMS.Hybrid.Services.Api;

namespace Giwu.HRMS.Hybrid.Services.Notifications;

public interface INotificationsApi
{
    Task<ApiResult<NotificationSummary>> ListAsync(bool unreadOnly = false, int limit = 50, CancellationToken ct = default);
    Task<ApiResult> MarkReadAsync(Guid id, CancellationToken ct = default);
    Task<ApiResult> MarkAllReadAsync(CancellationToken ct = default);
}

public sealed class NotificationsApiClient(HttpClient http) : INotificationsApi
{
    public Task<ApiResult<NotificationSummary>> ListAsync(bool unreadOnly = false, int limit = 50, CancellationToken ct = default)
    {
        var qs = $"?unreadOnly={unreadOnly}&limit={limit}";
        return ApiClientCore.GetAsync<NotificationSummary>(http, "/api/notifications" + qs, ct);
    }

    public Task<ApiResult> MarkReadAsync(Guid id, CancellationToken ct = default) =>
        ApiClientCore.PostEmptyAsync(http, $"/api/notifications/{id}/mark-read", ct);

    public Task<ApiResult> MarkAllReadAsync(CancellationToken ct = default) =>
        ApiClientCore.PostEmptyAsync(http, "/api/notifications/mark-all-read", ct);
}
