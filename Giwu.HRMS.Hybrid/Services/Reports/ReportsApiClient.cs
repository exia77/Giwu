using Giwu.Contracts.Reports;
using Giwu.HRMS.Hybrid.Services.Api;

namespace Giwu.HRMS.Hybrid.Services.Reports;

public interface IReportsApi
{
    // ── Live snapshot ────
    Task<ApiResult<HeadcountSummaryDto>> HeadcountAsync(CancellationToken ct = default);
    Task<ApiResult<AttendanceSummaryDto>> AttendanceAsync(DateOnly? from = null, DateOnly? to = null, CancellationToken ct = default);
    Task<ApiResult<LeaveSummaryDto>> LeaveAsync(CancellationToken ct = default);
    Task<ApiResult<PayrollSummaryDto>> PayrollAsync(int? year = null, CancellationToken ct = default);

    // ── Custom report definitions (built-ins are in code) ────
    Task<ApiResult<IReadOnlyList<ReportDefinitionDto>>> ListDefinitionsAsync(CancellationToken ct = default);
    Task<ApiResult<ReportDefinitionDto>> CreateDefinitionAsync(CreateReportDefinitionRequest body, CancellationToken ct = default);
    Task<ApiResult> UpdateDefinitionAsync(Guid id, UpdateReportDefinitionRequest body, CancellationToken ct = default);
    Task<ApiResult> DeleteDefinitionAsync(Guid id, CancellationToken ct = default);

    // ── Schedules ────
    Task<ApiResult<IReadOnlyList<ReportScheduleDto>>> ListSchedulesAsync(CancellationToken ct = default);
    Task<ApiResult<ReportScheduleDto>> CreateScheduleAsync(CreateReportScheduleRequest body, CancellationToken ct = default);
    Task<ApiResult> UpdateScheduleAsync(Guid id, UpdateReportScheduleRequest body, CancellationToken ct = default);
    Task<ApiResult> ToggleScheduleAsync(Guid id, CancellationToken ct = default);
    Task<ApiResult> DeleteScheduleAsync(Guid id, CancellationToken ct = default);

    // ── Runs ────
    Task<ApiResult<IReadOnlyList<ReportRunDto>>> ListRunsAsync(int limit = 200, CancellationToken ct = default);
    Task<ApiResult<ReportRunDto>> QueueRunAsync(QueueReportRunRequest body, CancellationToken ct = default);

    // ── Compliance ────
    Task<ApiResult<IReadOnlyList<ComplianceDeadlineDto>>> ListComplianceAsync(CancellationToken ct = default);
    Task<ApiResult<ComplianceDeadlineDto>> CreateComplianceAsync(CreateComplianceDeadlineRequest body, CancellationToken ct = default);
    Task<ApiResult> UpdateComplianceAsync(Guid id, UpdateComplianceDeadlineRequest body, CancellationToken ct = default);
    Task<ApiResult> SetComplianceFiledAsync(Guid id, bool filed, CancellationToken ct = default);
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

    // ── Definitions ────
    public Task<ApiResult<IReadOnlyList<ReportDefinitionDto>>> ListDefinitionsAsync(CancellationToken ct = default) =>
        ApiClientCore.GetAsync<IReadOnlyList<ReportDefinitionDto>>(http, "/api/reports/definitions", ct);

    public Task<ApiResult<ReportDefinitionDto>> CreateDefinitionAsync(CreateReportDefinitionRequest body, CancellationToken ct = default) =>
        ApiClientCore.PostAsync<CreateReportDefinitionRequest, ReportDefinitionDto>(http, "/api/reports/definitions", body, ct);

    public Task<ApiResult> UpdateDefinitionAsync(Guid id, UpdateReportDefinitionRequest body, CancellationToken ct = default) =>
        ApiClientCore.PutAsync(http, $"/api/reports/definitions/{id}", body, ct);

    public Task<ApiResult> DeleteDefinitionAsync(Guid id, CancellationToken ct = default) =>
        ApiClientCore.DeleteAsync(http, $"/api/reports/definitions/{id}", ct);

    // ── Schedules ────
    public Task<ApiResult<IReadOnlyList<ReportScheduleDto>>> ListSchedulesAsync(CancellationToken ct = default) =>
        ApiClientCore.GetAsync<IReadOnlyList<ReportScheduleDto>>(http, "/api/reports/schedules", ct);

    public Task<ApiResult<ReportScheduleDto>> CreateScheduleAsync(CreateReportScheduleRequest body, CancellationToken ct = default) =>
        ApiClientCore.PostAsync<CreateReportScheduleRequest, ReportScheduleDto>(http, "/api/reports/schedules", body, ct);

    public Task<ApiResult> UpdateScheduleAsync(Guid id, UpdateReportScheduleRequest body, CancellationToken ct = default) =>
        ApiClientCore.PutAsync(http, $"/api/reports/schedules/{id}", body, ct);

    public Task<ApiResult> ToggleScheduleAsync(Guid id, CancellationToken ct = default) =>
        ApiClientCore.PostEmptyAsync(http, $"/api/reports/schedules/{id}/toggle", ct);

    public Task<ApiResult> DeleteScheduleAsync(Guid id, CancellationToken ct = default) =>
        ApiClientCore.DeleteAsync(http, $"/api/reports/schedules/{id}", ct);

    // ── Runs ────
    public Task<ApiResult<IReadOnlyList<ReportRunDto>>> ListRunsAsync(int limit = 200, CancellationToken ct = default) =>
        ApiClientCore.GetAsync<IReadOnlyList<ReportRunDto>>(http, $"/api/reports/runs?limit={limit}", ct);

    public Task<ApiResult<ReportRunDto>> QueueRunAsync(QueueReportRunRequest body, CancellationToken ct = default) =>
        ApiClientCore.PostAsync<QueueReportRunRequest, ReportRunDto>(http, "/api/reports/runs", body, ct);

    // ── Compliance ────
    public Task<ApiResult<IReadOnlyList<ComplianceDeadlineDto>>> ListComplianceAsync(CancellationToken ct = default) =>
        ApiClientCore.GetAsync<IReadOnlyList<ComplianceDeadlineDto>>(http, "/api/reports/compliance", ct);

    public Task<ApiResult<ComplianceDeadlineDto>> CreateComplianceAsync(CreateComplianceDeadlineRequest body, CancellationToken ct = default) =>
        ApiClientCore.PostAsync<CreateComplianceDeadlineRequest, ComplianceDeadlineDto>(http, "/api/reports/compliance", body, ct);

    public Task<ApiResult> UpdateComplianceAsync(Guid id, UpdateComplianceDeadlineRequest body, CancellationToken ct = default) =>
        ApiClientCore.PutAsync(http, $"/api/reports/compliance/{id}", body, ct);

    public Task<ApiResult> SetComplianceFiledAsync(Guid id, bool filed, CancellationToken ct = default) =>
        ApiClientCore.PostAsync(http, $"/api/reports/compliance/{id}/filed", new { Filed = filed }, ct);
}
