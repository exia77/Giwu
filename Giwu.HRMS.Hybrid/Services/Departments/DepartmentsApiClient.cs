using Giwu.Contracts.Departments;
using Giwu.HRMS.Hybrid.Services.Api;

namespace Giwu.HRMS.Hybrid.Services.Departments;

public interface IDepartmentsApi
{
    Task<ApiResult<IReadOnlyList<DepartmentDto>>> ListAsync(bool includeInactive = false, CancellationToken ct = default);
    Task<ApiResult<DepartmentDto>> GetAsync(Guid id, CancellationToken ct = default);
    Task<ApiResult<DepartmentDto>> CreateAsync(CreateDepartmentRequest body, CancellationToken ct = default);
    Task<ApiResult> UpdateAsync(Guid id, UpdateDepartmentRequest body, CancellationToken ct = default);
    Task<ApiResult> DeleteAsync(Guid id, CancellationToken ct = default);
}

public sealed class DepartmentsApiClient(HttpClient http) : IDepartmentsApi
{
    public Task<ApiResult<IReadOnlyList<DepartmentDto>>> ListAsync(bool includeInactive = false, CancellationToken ct = default) =>
        ApiClientCore.GetAsync<IReadOnlyList<DepartmentDto>>(http, $"/api/departments?includeInactive={includeInactive}", ct);

    public Task<ApiResult<DepartmentDto>> GetAsync(Guid id, CancellationToken ct = default) =>
        ApiClientCore.GetAsync<DepartmentDto>(http, $"/api/departments/{id}", ct);

    public Task<ApiResult<DepartmentDto>> CreateAsync(CreateDepartmentRequest body, CancellationToken ct = default) =>
        ApiClientCore.PostAsync<CreateDepartmentRequest, DepartmentDto>(http, "/api/departments", body, ct);

    public Task<ApiResult> UpdateAsync(Guid id, UpdateDepartmentRequest body, CancellationToken ct = default) =>
        ApiClientCore.PutAsync(http, $"/api/departments/{id}", body, ct);

    public Task<ApiResult> DeleteAsync(Guid id, CancellationToken ct = default) =>
        ApiClientCore.DeleteAsync(http, $"/api/departments/{id}", ct);
}
