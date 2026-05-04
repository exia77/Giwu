using Giwu.Contracts.Common;
using Giwu.Contracts.Employees;
using Giwu.HRMS.Hybrid.Services.Api;

namespace Giwu.HRMS.Hybrid.Services.Employees;

public interface IEmployeesApi
{
    Task<ApiResult<PagedResult<EmployeeDto>>> ListAsync(int page = 1, int pageSize = 25, string? search = null, CancellationToken ct = default);
    Task<ApiResult<EmployeeDto>> GetAsync(Guid id, CancellationToken ct = default);
    Task<ApiResult<EmployeeDto>> CreateAsync(CreateEmployeeRequest body, CancellationToken ct = default);
    Task<ApiResult> UpdateAsync(Guid id, UpdateEmployeeRequest body, CancellationToken ct = default);
    Task<ApiResult> DeleteAsync(Guid id, CancellationToken ct = default);
}

public sealed class EmployeesApiClient(HttpClient http) : IEmployeesApi
{
    public Task<ApiResult<PagedResult<EmployeeDto>>> ListAsync(
        int page = 1, int pageSize = 25, string? search = null, CancellationToken ct = default)
    {
        var qs = $"?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(search))
            qs += $"&search={Uri.EscapeDataString(search)}";
        return ApiClientCore.GetAsync<PagedResult<EmployeeDto>>(http, "/api/employees" + qs, ct);
    }

    public Task<ApiResult<EmployeeDto>> GetAsync(Guid id, CancellationToken ct = default) =>
        ApiClientCore.GetAsync<EmployeeDto>(http, $"/api/employees/{id}", ct);

    public Task<ApiResult<EmployeeDto>> CreateAsync(CreateEmployeeRequest body, CancellationToken ct = default) =>
        ApiClientCore.PostAsync<CreateEmployeeRequest, EmployeeDto>(http, "/api/employees", body, ct);

    public Task<ApiResult> UpdateAsync(Guid id, UpdateEmployeeRequest body, CancellationToken ct = default) =>
        ApiClientCore.PutAsync(http, $"/api/employees/{id}", body, ct);

    public Task<ApiResult> DeleteAsync(Guid id, CancellationToken ct = default) =>
        ApiClientCore.DeleteAsync(http, $"/api/employees/{id}", ct);
}
