using Giwu.Contracts.Reports;
using Giwu.HRMS.Hybrid.Services.Api;

namespace Giwu.HRMS.Hybrid.Services.Reports;

public interface IReportsApi
{
    Task<ApiResult<HeadcountSummaryDto>> HeadcountAsync(CancellationToken ct = default);
    Task<ApiResult<AttendanceSummaryDto>> AttendanceAsync(DateOnly? from = null, DateOnly? to = null, CancellationToken ct = default);
    Task<ApiResult<LeaveSummaryDto>> LeaveAsync(CancellationToken ct = default);
    Task<ApiResult<PayrollSummaryDto>> PayrollAsync(int? year = null, CancellationToken ct = default);
}

public sealed class ReportsApiClient(HttpClient http) : IReportsApi
{
    public Task<ApiResult<HeadcountSummaryDto>> HeadcountAsync(CancellationToken ct = default) =>
        ApiClientCore.GetAsync<HeadcountSummaryDto>(http, "/api/reports/headcount", ct);

    public Task<ApiResult<AttendanceSummaryDto>> AttendanceAsync(DateOnly? from = null, DateOnly? to = null, CancellationToken ct = default)
    {
        var qs = new List<string>();
        if (from.HasValue) qs.Add($"from={from.Value:yyyy-MM-dd}");
        if (to.HasValue) qs.Add($"to={to.Value:yyyy-MM-dd}");
        var path = qs.Count == 0 ? "/api/reports/attendance" : "/api/reports/attendance?" + string.Join("&", qs);
        return ApiClientCore.GetAsync<AttendanceSummaryDto>(http, path, ct);
    }

    public Task<ApiResult<LeaveSummaryDto>> LeaveAsync(CancellationToken ct = default) =>
        ApiClientCore.GetAsync<LeaveSummaryDto>(http, "/api/reports/leave", ct);

    public Task<ApiResult<PayrollSummaryDto>> PayrollAsync(int? year = null, CancellationToken ct = default) =>
        ApiClientCore.GetAsync<PayrollSummaryDto>(http,
            year.HasValue ? $"/api/reports/payroll?year={year.Value}" : "/api/reports/payroll", ct);
}
