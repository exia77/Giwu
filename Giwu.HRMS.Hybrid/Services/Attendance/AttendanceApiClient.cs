using Giwu.Contracts.Attendance;
using Giwu.Contracts.Common;
using Giwu.Domain.Attendance;
using Giwu.HRMS.Hybrid.Services.Api;

namespace Giwu.HRMS.Hybrid.Services.Attendance;

public interface IAttendanceApi
{
    Task<ApiResult<AttendanceRecordDto>> ClockInAsync(ClockInRequest body, CancellationToken ct = default);
    Task<ApiResult<AttendanceRecordDto>> ClockOutAsync(ClockOutRequest body, CancellationToken ct = default);

    Task<ApiResult<PagedResult<AttendanceRecordDto>>> ListAsync(
        DateOnly? from = null, DateOnly? to = null, Guid? employeeId = null,
        int page = 1, int pageSize = 50, CancellationToken ct = default);

    Task<ApiResult<AttendanceRecordDto>> GetTodayAsync(Guid? employeeId = null, CancellationToken ct = default);

    Task<ApiResult<AttendanceRecordDto>> UpdateAsync(Guid id, UpdateAttendanceRequest body, CancellationToken ct = default);
    Task<ApiResult<AttendanceRecordDto>> CreateManualAsync(ManualAttendanceEntryRequest body, CancellationToken ct = default);

    Task<ApiResult<IReadOnlyList<OvertimeRequestDto>>> ListOvertimeAsync(
        ApprovalStatus? status = null, Guid? employeeId = null, bool mineOnly = false, CancellationToken ct = default);

    Task<ApiResult<OvertimeRequestDto>> FileOvertimeAsync(FileOvertimeRequest body, CancellationToken ct = default);
    Task<ApiResult> ApproveOvertimeAsync(Guid id, ResolveOvertimeRequest body, CancellationToken ct = default);
    Task<ApiResult> RejectOvertimeAsync(Guid id, ResolveOvertimeRequest body, CancellationToken ct = default);
}

public sealed class AttendanceApiClient(HttpClient http) : IAttendanceApi
{
    public Task<ApiResult<AttendanceRecordDto>> ClockInAsync(ClockInRequest body, CancellationToken ct = default) =>
        ApiClientCore.PostAsync<ClockInRequest, AttendanceRecordDto>(http, "/api/attendance/clock-in", body, ct);

    public Task<ApiResult<AttendanceRecordDto>> ClockOutAsync(ClockOutRequest body, CancellationToken ct = default) =>
        ApiClientCore.PostAsync<ClockOutRequest, AttendanceRecordDto>(http, "/api/attendance/clock-out", body, ct);

    public Task<ApiResult<PagedResult<AttendanceRecordDto>>> ListAsync(
        DateOnly? from = null, DateOnly? to = null, Guid? employeeId = null,
        int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var qs = new List<string> { $"page={page}", $"pageSize={pageSize}" };
        if (from.HasValue)       qs.Add($"from={from.Value:yyyy-MM-dd}");
        if (to.HasValue)         qs.Add($"to={to.Value:yyyy-MM-dd}");
        if (employeeId.HasValue) qs.Add($"employeeId={employeeId.Value}");
        return ApiClientCore.GetAsync<PagedResult<AttendanceRecordDto>>(http, "/api/attendance?" + string.Join("&", qs), ct);
    }

    public Task<ApiResult<AttendanceRecordDto>> GetTodayAsync(Guid? employeeId = null, CancellationToken ct = default)
    {
        var qs = employeeId.HasValue ? $"?employeeId={employeeId.Value}" : string.Empty;
        return ApiClientCore.GetAsync<AttendanceRecordDto>(http, "/api/attendance/today" + qs, ct);
    }

    public Task<ApiResult<AttendanceRecordDto>> UpdateAsync(Guid id, UpdateAttendanceRequest body, CancellationToken ct = default) =>
        ApiClientCore.PutAsync<UpdateAttendanceRequest, AttendanceRecordDto>(http, $"/api/attendance/{id}", body, ct);

    public Task<ApiResult<AttendanceRecordDto>> CreateManualAsync(ManualAttendanceEntryRequest body, CancellationToken ct = default) =>
        ApiClientCore.PostAsync<ManualAttendanceEntryRequest, AttendanceRecordDto>(http, "/api/attendance/manual", body, ct);

    public Task<ApiResult<IReadOnlyList<OvertimeRequestDto>>> ListOvertimeAsync(
        ApprovalStatus? status = null, Guid? employeeId = null, bool mineOnly = false, CancellationToken ct = default)
    {
        var qs = new List<string> { $"mineOnly={mineOnly}" };
        if (status.HasValue)     qs.Add($"status={status.Value}");
        if (employeeId.HasValue) qs.Add($"employeeId={employeeId.Value}");
        return ApiClientCore.GetAsync<IReadOnlyList<OvertimeRequestDto>>(http, "/api/overtime-requests?" + string.Join("&", qs), ct);
    }

    public Task<ApiResult<OvertimeRequestDto>> FileOvertimeAsync(FileOvertimeRequest body, CancellationToken ct = default) =>
        ApiClientCore.PostAsync<FileOvertimeRequest, OvertimeRequestDto>(http, "/api/overtime-requests", body, ct);

    public Task<ApiResult> ApproveOvertimeAsync(Guid id, ResolveOvertimeRequest body, CancellationToken ct = default) =>
        ApiClientCore.PostAsync(http, $"/api/overtime-requests/{id}/approve", body, ct);

    public Task<ApiResult> RejectOvertimeAsync(Guid id, ResolveOvertimeRequest body, CancellationToken ct = default) =>
        ApiClientCore.PostAsync(http, $"/api/overtime-requests/{id}/reject", body, ct);
}
