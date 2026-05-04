using Giwu.Contracts.Common;
using Giwu.Contracts.Leaves;
using Giwu.Domain.Leaves;
using Giwu.HRMS.Hybrid.Services.Api;

namespace Giwu.HRMS.Hybrid.Services.Leaves;

public interface ILeavesApi
{
    Task<ApiResult<PagedResult<LeaveRequestDto>>> ListRequestsAsync(
        int page = 1, int pageSize = 25,
        LeaveRequestStatus? status = null, Guid? employeeId = null, bool mineOnly = false,
        CancellationToken ct = default);

    Task<ApiResult<LeaveRequestDto>> GetRequestAsync(Guid id, CancellationToken ct = default);
    Task<ApiResult<LeaveRequestDto>> FileRequestAsync(FileLeaveRequest body, CancellationToken ct = default);
    Task<ApiResult> ApproveRequestAsync(Guid id, ApproveLeaveRequest body, CancellationToken ct = default);
    Task<ApiResult> RejectRequestAsync(Guid id, RejectLeaveRequest body, CancellationToken ct = default);
    Task<ApiResult> CancelRequestAsync(Guid id, CancellationToken ct = default);

    Task<ApiResult<IReadOnlyList<LeaveBalanceDto>>> ListBalancesAsync(Guid? employeeId = null, int? year = null, CancellationToken ct = default);

    Task<ApiResult<IReadOnlyList<LeaveTypeDto>>> ListTypesAsync(bool includeInactive = false, CancellationToken ct = default);
    Task<ApiResult<LeaveTypeDto>> CreateTypeAsync(CreateLeaveTypeRequest body, CancellationToken ct = default);
    Task<ApiResult> UpdateTypeAsync(Guid id, UpdateLeaveTypeRequest body, CancellationToken ct = default);
    Task<ApiResult> DeleteTypeAsync(Guid id, CancellationToken ct = default);
}

public sealed class LeavesApiClient(HttpClient http) : ILeavesApi
{
    public Task<ApiResult<PagedResult<LeaveRequestDto>>> ListRequestsAsync(
        int page = 1, int pageSize = 25,
        LeaveRequestStatus? status = null, Guid? employeeId = null, bool mineOnly = false,
        CancellationToken ct = default)
    {
        var qs = new List<string> { $"page={page}", $"pageSize={pageSize}", $"mineOnly={mineOnly}" };
        if (status.HasValue)     qs.Add($"status={status.Value}");
        if (employeeId.HasValue) qs.Add($"employeeId={employeeId.Value}");
        return ApiClientCore.GetAsync<PagedResult<LeaveRequestDto>>(http, "/api/leave-requests?" + string.Join("&", qs), ct);
    }

    public Task<ApiResult<LeaveRequestDto>> GetRequestAsync(Guid id, CancellationToken ct = default) =>
        ApiClientCore.GetAsync<LeaveRequestDto>(http, $"/api/leave-requests/{id}", ct);

    public Task<ApiResult<LeaveRequestDto>> FileRequestAsync(FileLeaveRequest body, CancellationToken ct = default) =>
        ApiClientCore.PostAsync<FileLeaveRequest, LeaveRequestDto>(http, "/api/leave-requests", body, ct);

    public Task<ApiResult> ApproveRequestAsync(Guid id, ApproveLeaveRequest body, CancellationToken ct = default) =>
        ApiClientCore.PostAsync(http, $"/api/leave-requests/{id}/approve", body, ct);

    public Task<ApiResult> RejectRequestAsync(Guid id, RejectLeaveRequest body, CancellationToken ct = default) =>
        ApiClientCore.PostAsync(http, $"/api/leave-requests/{id}/reject", body, ct);

    public Task<ApiResult> CancelRequestAsync(Guid id, CancellationToken ct = default) =>
        ApiClientCore.PostEmptyAsync(http, $"/api/leave-requests/{id}/cancel", ct);

    public Task<ApiResult<IReadOnlyList<LeaveBalanceDto>>> ListBalancesAsync(
        Guid? employeeId = null, int? year = null, CancellationToken ct = default)
    {
        var qs = new List<string>();
        if (employeeId.HasValue) qs.Add($"employeeId={employeeId.Value}");
        if (year.HasValue)       qs.Add($"year={year.Value}");
        var path = qs.Count == 0 ? "/api/leave-balances" : "/api/leave-balances?" + string.Join("&", qs);
        return ApiClientCore.GetAsync<IReadOnlyList<LeaveBalanceDto>>(http, path, ct);
    }

    public Task<ApiResult<IReadOnlyList<LeaveTypeDto>>> ListTypesAsync(bool includeInactive = false, CancellationToken ct = default) =>
        ApiClientCore.GetAsync<IReadOnlyList<LeaveTypeDto>>(http, $"/api/leave-types?includeInactive={includeInactive}", ct);

    public Task<ApiResult<LeaveTypeDto>> CreateTypeAsync(CreateLeaveTypeRequest body, CancellationToken ct = default) =>
        ApiClientCore.PostAsync<CreateLeaveTypeRequest, LeaveTypeDto>(http, "/api/leave-types", body, ct);

    public Task<ApiResult> UpdateTypeAsync(Guid id, UpdateLeaveTypeRequest body, CancellationToken ct = default) =>
        ApiClientCore.PutAsync(http, $"/api/leave-types/{id}", body, ct);

    public Task<ApiResult> DeleteTypeAsync(Guid id, CancellationToken ct = default) =>
        ApiClientCore.DeleteAsync(http, $"/api/leave-types/{id}", ct);
}
