using Giwu.Contracts.Benefits;
using Giwu.Domain.Benefits;
using Giwu.HRMS.Hybrid.Services.Api;

namespace Giwu.HRMS.Hybrid.Services.Benefits;

public interface IBenefitsApi
{
    Task<ApiResult<IReadOnlyList<BenefitProgramDto>>> ListProgramsAsync(
        bool includeInactive = false, BenefitCategory? category = null, CancellationToken ct = default);
    Task<ApiResult<BenefitProgramDto>> GetProgramAsync(Guid id, CancellationToken ct = default);
    Task<ApiResult<BenefitProgramDto>> CreateProgramAsync(CreateBenefitProgramRequest body, CancellationToken ct = default);
    Task<ApiResult> UpdateProgramAsync(Guid id, UpdateBenefitProgramRequest body, CancellationToken ct = default);
    Task<ApiResult> DeleteProgramAsync(Guid id, CancellationToken ct = default);

    Task<ApiResult<IReadOnlyList<BenefitEnrollmentDto>>> ListEnrollmentsAsync(
        Guid? employeeId = null, Guid? programId = null, EnrollmentStatus? status = null, CancellationToken ct = default);
    Task<ApiResult<BenefitEnrollmentDto>> CreateEnrollmentAsync(CreateBenefitEnrollmentRequest body, CancellationToken ct = default);
    Task<ApiResult> UpdateEnrollmentAsync(Guid id, UpdateBenefitEnrollmentRequest body, CancellationToken ct = default);
    Task<ApiResult> DeleteEnrollmentAsync(Guid id, CancellationToken ct = default);

    Task<ApiResult<IReadOnlyList<BenefitRequestDto>>> ListRequestsAsync(
        Guid? employeeId = null, BenefitRequestStatus? status = null, BenefitRequestType? type = null,
        CancellationToken ct = default);
    Task<ApiResult<BenefitRequestDto>> FileRequestAsync(FileBenefitRequestRequest body, CancellationToken ct = default);
    Task<ApiResult> ResolveRequestAsync(Guid id, ResolveBenefitRequestRequest body, CancellationToken ct = default);
}

public sealed class BenefitsApiClient(HttpClient http) : IBenefitsApi
{
    public Task<ApiResult<IReadOnlyList<BenefitProgramDto>>> ListProgramsAsync(
        bool includeInactive = false, BenefitCategory? category = null, CancellationToken ct = default)
    {
        var qs = new List<string> { $"includeInactive={includeInactive}" };
        if (category.HasValue) qs.Add($"category={category.Value}");
        return ApiClientCore.GetAsync<IReadOnlyList<BenefitProgramDto>>(
            http, "/api/benefits/programs?" + string.Join("&", qs), ct);
    }

    public Task<ApiResult<BenefitProgramDto>> GetProgramAsync(Guid id, CancellationToken ct = default) =>
        ApiClientCore.GetAsync<BenefitProgramDto>(http, $"/api/benefits/programs/{id}", ct);

    public Task<ApiResult<BenefitProgramDto>> CreateProgramAsync(CreateBenefitProgramRequest body, CancellationToken ct = default) =>
        ApiClientCore.PostAsync<CreateBenefitProgramRequest, BenefitProgramDto>(http, "/api/benefits/programs", body, ct);

    public Task<ApiResult> UpdateProgramAsync(Guid id, UpdateBenefitProgramRequest body, CancellationToken ct = default) =>
        ApiClientCore.PutAsync(http, $"/api/benefits/programs/{id}", body, ct);

    public Task<ApiResult> DeleteProgramAsync(Guid id, CancellationToken ct = default) =>
        ApiClientCore.DeleteAsync(http, $"/api/benefits/programs/{id}", ct);

    public Task<ApiResult<IReadOnlyList<BenefitEnrollmentDto>>> ListEnrollmentsAsync(
        Guid? employeeId = null, Guid? programId = null, EnrollmentStatus? status = null, CancellationToken ct = default)
    {
        var qs = new List<string>();
        if (employeeId.HasValue) qs.Add($"employeeId={employeeId.Value}");
        if (programId.HasValue) qs.Add($"programId={programId.Value}");
        if (status.HasValue) qs.Add($"status={status.Value}");
        var path = qs.Count == 0 ? "/api/benefits/enrollments" : "/api/benefits/enrollments?" + string.Join("&", qs);
        return ApiClientCore.GetAsync<IReadOnlyList<BenefitEnrollmentDto>>(http, path, ct);
    }

    public Task<ApiResult<BenefitEnrollmentDto>> CreateEnrollmentAsync(CreateBenefitEnrollmentRequest body, CancellationToken ct = default) =>
        ApiClientCore.PostAsync<CreateBenefitEnrollmentRequest, BenefitEnrollmentDto>(http, "/api/benefits/enrollments", body, ct);

    public Task<ApiResult> UpdateEnrollmentAsync(Guid id, UpdateBenefitEnrollmentRequest body, CancellationToken ct = default) =>
        ApiClientCore.PutAsync(http, $"/api/benefits/enrollments/{id}", body, ct);

    public Task<ApiResult> DeleteEnrollmentAsync(Guid id, CancellationToken ct = default) =>
        ApiClientCore.DeleteAsync(http, $"/api/benefits/enrollments/{id}", ct);

    public Task<ApiResult<IReadOnlyList<BenefitRequestDto>>> ListRequestsAsync(
        Guid? employeeId = null, BenefitRequestStatus? status = null, BenefitRequestType? type = null,
        CancellationToken ct = default)
    {
        var qs = new List<string>();
        if (employeeId.HasValue) qs.Add($"employeeId={employeeId.Value}");
        if (status.HasValue) qs.Add($"status={status.Value}");
        if (type.HasValue) qs.Add($"type={type.Value}");
        var path = qs.Count == 0 ? "/api/benefits/requests" : "/api/benefits/requests?" + string.Join("&", qs);
        return ApiClientCore.GetAsync<IReadOnlyList<BenefitRequestDto>>(http, path, ct);
    }

    public Task<ApiResult<BenefitRequestDto>> FileRequestAsync(FileBenefitRequestRequest body, CancellationToken ct = default) =>
        ApiClientCore.PostAsync<FileBenefitRequestRequest, BenefitRequestDto>(http, "/api/benefits/requests", body, ct);

    public Task<ApiResult> ResolveRequestAsync(Guid id, ResolveBenefitRequestRequest body, CancellationToken ct = default) =>
        ApiClientCore.PostAsync(http, $"/api/benefits/requests/{id}/resolve", body, ct);
}
