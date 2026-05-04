using Giwu.Contracts.Recruitment;
using Giwu.Domain.Recruitment;
using Giwu.HRMS.Hybrid.Services.Api;

namespace Giwu.HRMS.Hybrid.Services.Recruitment;

public interface IRecruitmentApi
{
    // Jobs
    Task<ApiResult<IReadOnlyList<JobRequisitionDto>>> ListJobsAsync(
        JobStatus? status = null, Guid? departmentId = null, string? search = null, CancellationToken ct = default);
    Task<ApiResult<JobRequisitionDto>> GetJobAsync(Guid id, CancellationToken ct = default);
    Task<ApiResult<JobRequisitionDto>> CreateJobAsync(CreateJobRequisitionRequest body, CancellationToken ct = default);
    Task<ApiResult> UpdateJobAsync(Guid id, UpdateJobRequisitionRequest body, CancellationToken ct = default);
    Task<ApiResult> ChangeJobStatusAsync(Guid id, ChangeJobStatusRequest body, CancellationToken ct = default);
    Task<ApiResult> DeleteJobAsync(Guid id, CancellationToken ct = default);

    // Candidates
    Task<ApiResult<IReadOnlyList<CandidateDto>>> ListCandidatesAsync(
        CandidateStage? stage = null, Guid? jobId = null, string? source = null,
        int? minRating = null, string? search = null, CancellationToken ct = default);
    Task<ApiResult<CandidateDto>> GetCandidateAsync(Guid id, CancellationToken ct = default);
    Task<ApiResult<CandidateDto>> CreateCandidateAsync(CreateCandidateRequest body, CancellationToken ct = default);
    Task<ApiResult> UpdateCandidateAsync(Guid id, UpdateCandidateRequest body, CancellationToken ct = default);
    Task<ApiResult> AdvanceCandidateAsync(Guid id, AdvanceCandidateRequest body, CancellationToken ct = default);
    Task<ApiResult> RejectCandidateAsync(Guid id, RejectCandidateRequest body, CancellationToken ct = default);
    Task<ApiResult> DeleteCandidateAsync(Guid id, CancellationToken ct = default);

    // Interviews
    Task<ApiResult<IReadOnlyList<InterviewDto>>> ListInterviewsAsync(
        Guid? candidateId = null, DateTimeOffset? from = null, DateTimeOffset? to = null,
        InterviewStatus? status = null, CancellationToken ct = default);
    Task<ApiResult<InterviewDto>> ScheduleInterviewAsync(ScheduleInterviewRequest body, CancellationToken ct = default);
    Task<ApiResult> UpdateInterviewAsync(Guid id, UpdateInterviewRequest body, CancellationToken ct = default);
    Task<ApiResult> CancelInterviewAsync(Guid id, CancellationToken ct = default);
}

public sealed class RecruitmentApiClient(HttpClient http) : IRecruitmentApi
{
    public Task<ApiResult<IReadOnlyList<JobRequisitionDto>>> ListJobsAsync(
        JobStatus? status = null, Guid? departmentId = null, string? search = null, CancellationToken ct = default)
    {
        var qs = new List<string>();
        if (status.HasValue) qs.Add($"status={status.Value}");
        if (departmentId.HasValue) qs.Add($"departmentId={departmentId.Value}");
        if (!string.IsNullOrWhiteSpace(search)) qs.Add($"search={Uri.EscapeDataString(search)}");
        var path = qs.Count == 0 ? "/api/recruitment/jobs" : "/api/recruitment/jobs?" + string.Join("&", qs);
        return ApiClientCore.GetAsync<IReadOnlyList<JobRequisitionDto>>(http, path, ct);
    }

    public Task<ApiResult<JobRequisitionDto>> GetJobAsync(Guid id, CancellationToken ct = default) =>
        ApiClientCore.GetAsync<JobRequisitionDto>(http, $"/api/recruitment/jobs/{id}", ct);

    public Task<ApiResult<JobRequisitionDto>> CreateJobAsync(CreateJobRequisitionRequest body, CancellationToken ct = default) =>
        ApiClientCore.PostAsync<CreateJobRequisitionRequest, JobRequisitionDto>(http, "/api/recruitment/jobs", body, ct);

    public Task<ApiResult> UpdateJobAsync(Guid id, UpdateJobRequisitionRequest body, CancellationToken ct = default) =>
        ApiClientCore.PutAsync(http, $"/api/recruitment/jobs/{id}", body, ct);

    public Task<ApiResult> ChangeJobStatusAsync(Guid id, ChangeJobStatusRequest body, CancellationToken ct = default) =>
        ApiClientCore.PostAsync(http, $"/api/recruitment/jobs/{id}/status", body, ct);

    public Task<ApiResult> DeleteJobAsync(Guid id, CancellationToken ct = default) =>
        ApiClientCore.DeleteAsync(http, $"/api/recruitment/jobs/{id}", ct);

    public Task<ApiResult<IReadOnlyList<CandidateDto>>> ListCandidatesAsync(
        CandidateStage? stage = null, Guid? jobId = null, string? source = null,
        int? minRating = null, string? search = null, CancellationToken ct = default)
    {
        var qs = new List<string>();
        if (stage.HasValue) qs.Add($"stage={stage.Value}");
        if (jobId.HasValue) qs.Add($"jobId={jobId.Value}");
        if (!string.IsNullOrWhiteSpace(source)) qs.Add($"source={Uri.EscapeDataString(source)}");
        if (minRating.HasValue) qs.Add($"minRating={minRating.Value}");
        if (!string.IsNullOrWhiteSpace(search)) qs.Add($"search={Uri.EscapeDataString(search)}");
        var path = qs.Count == 0 ? "/api/recruitment/candidates" : "/api/recruitment/candidates?" + string.Join("&", qs);
        return ApiClientCore.GetAsync<IReadOnlyList<CandidateDto>>(http, path, ct);
    }

    public Task<ApiResult<CandidateDto>> GetCandidateAsync(Guid id, CancellationToken ct = default) =>
        ApiClientCore.GetAsync<CandidateDto>(http, $"/api/recruitment/candidates/{id}", ct);

    public Task<ApiResult<CandidateDto>> CreateCandidateAsync(CreateCandidateRequest body, CancellationToken ct = default) =>
        ApiClientCore.PostAsync<CreateCandidateRequest, CandidateDto>(http, "/api/recruitment/candidates", body, ct);

    public Task<ApiResult> UpdateCandidateAsync(Guid id, UpdateCandidateRequest body, CancellationToken ct = default) =>
        ApiClientCore.PutAsync(http, $"/api/recruitment/candidates/{id}", body, ct);

    public Task<ApiResult> AdvanceCandidateAsync(Guid id, AdvanceCandidateRequest body, CancellationToken ct = default) =>
        ApiClientCore.PostAsync(http, $"/api/recruitment/candidates/{id}/advance", body, ct);

    public Task<ApiResult> RejectCandidateAsync(Guid id, RejectCandidateRequest body, CancellationToken ct = default) =>
        ApiClientCore.PostAsync(http, $"/api/recruitment/candidates/{id}/reject", body, ct);

    public Task<ApiResult> DeleteCandidateAsync(Guid id, CancellationToken ct = default) =>
        ApiClientCore.DeleteAsync(http, $"/api/recruitment/candidates/{id}", ct);

    public Task<ApiResult<IReadOnlyList<InterviewDto>>> ListInterviewsAsync(
        Guid? candidateId = null, DateTimeOffset? from = null, DateTimeOffset? to = null,
        InterviewStatus? status = null, CancellationToken ct = default)
    {
        var qs = new List<string>();
        if (candidateId.HasValue) qs.Add($"candidateId={candidateId.Value}");
        if (from.HasValue) qs.Add($"from={Uri.EscapeDataString(from.Value.ToString("o"))}");
        if (to.HasValue) qs.Add($"to={Uri.EscapeDataString(to.Value.ToString("o"))}");
        if (status.HasValue) qs.Add($"status={status.Value}");
        var path = qs.Count == 0 ? "/api/recruitment/interviews" : "/api/recruitment/interviews?" + string.Join("&", qs);
        return ApiClientCore.GetAsync<IReadOnlyList<InterviewDto>>(http, path, ct);
    }

    public Task<ApiResult<InterviewDto>> ScheduleInterviewAsync(ScheduleInterviewRequest body, CancellationToken ct = default) =>
        ApiClientCore.PostAsync<ScheduleInterviewRequest, InterviewDto>(http, "/api/recruitment/interviews", body, ct);

    public Task<ApiResult> UpdateInterviewAsync(Guid id, UpdateInterviewRequest body, CancellationToken ct = default) =>
        ApiClientCore.PutAsync(http, $"/api/recruitment/interviews/{id}", body, ct);

    public Task<ApiResult> CancelInterviewAsync(Guid id, CancellationToken ct = default) =>
        ApiClientCore.PostEmptyAsync(http, $"/api/recruitment/interviews/{id}/cancel", ct);
}
