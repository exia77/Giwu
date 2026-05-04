using Giwu.Contracts.Payroll;
using Giwu.Domain.Payroll;
using Giwu.HRMS.Hybrid.Services.Api;

namespace Giwu.HRMS.Hybrid.Services.Payroll;

public interface IPayrollApi
{
    Task<ApiResult<IReadOnlyList<PayPeriodDto>>> ListPeriodsAsync(int? year = null, PayPeriodStatus? status = null, CancellationToken ct = default);
    Task<ApiResult<PayPeriodDto>> GetPeriodAsync(Guid id, CancellationToken ct = default);
    Task<ApiResult<PayPeriodDto>> CreatePeriodAsync(CreatePayPeriodRequest body, CancellationToken ct = default);
    Task<ApiResult> UpdatePeriodAsync(Guid id, UpdatePayPeriodRequest body, CancellationToken ct = default);
    Task<ApiResult> ApprovePeriodAsync(Guid id, ApprovePayPeriodRequest body, CancellationToken ct = default);
    Task<ApiResult> ReleasePeriodAsync(Guid id, CancellationToken ct = default);
    Task<ApiResult> DeletePeriodAsync(Guid id, CancellationToken ct = default);

    Task<ApiResult<IReadOnlyList<PayslipDto>>> ListPayslipsAsync(Guid? payPeriodId = null, Guid? employeeId = null, CancellationToken ct = default);
    Task<ApiResult<PayslipDto>> GetPayslipAsync(Guid id, CancellationToken ct = default);
    Task<ApiResult<PayslipDto>> CreatePayslipAsync(CreatePayslipRequest body, CancellationToken ct = default);
    Task<ApiResult> UpdatePayslipAsync(Guid id, UpdatePayslipRequest body, CancellationToken ct = default);
    Task<ApiResult> DeletePayslipAsync(Guid id, CancellationToken ct = default);
}

public sealed class PayrollApiClient(HttpClient http) : IPayrollApi
{
    public Task<ApiResult<IReadOnlyList<PayPeriodDto>>> ListPeriodsAsync(int? year = null, PayPeriodStatus? status = null, CancellationToken ct = default)
    {
        var qs = new List<string>();
        if (year.HasValue) qs.Add($"year={year.Value}");
        if (status.HasValue) qs.Add($"status={status.Value}");
        var path = qs.Count == 0 ? "/api/payroll/periods" : "/api/payroll/periods?" + string.Join("&", qs);
        return ApiClientCore.GetAsync<IReadOnlyList<PayPeriodDto>>(http, path, ct);
    }

    public Task<ApiResult<PayPeriodDto>> GetPeriodAsync(Guid id, CancellationToken ct = default) =>
        ApiClientCore.GetAsync<PayPeriodDto>(http, $"/api/payroll/periods/{id}", ct);

    public Task<ApiResult<PayPeriodDto>> CreatePeriodAsync(CreatePayPeriodRequest body, CancellationToken ct = default) =>
        ApiClientCore.PostAsync<CreatePayPeriodRequest, PayPeriodDto>(http, "/api/payroll/periods", body, ct);

    public Task<ApiResult> UpdatePeriodAsync(Guid id, UpdatePayPeriodRequest body, CancellationToken ct = default) =>
        ApiClientCore.PutAsync(http, $"/api/payroll/periods/{id}", body, ct);

    public Task<ApiResult> ApprovePeriodAsync(Guid id, ApprovePayPeriodRequest body, CancellationToken ct = default) =>
        ApiClientCore.PostAsync(http, $"/api/payroll/periods/{id}/approve", body, ct);

    public Task<ApiResult> ReleasePeriodAsync(Guid id, CancellationToken ct = default) =>
        ApiClientCore.PostEmptyAsync(http, $"/api/payroll/periods/{id}/release", ct);

    public Task<ApiResult> DeletePeriodAsync(Guid id, CancellationToken ct = default) =>
        ApiClientCore.DeleteAsync(http, $"/api/payroll/periods/{id}", ct);

    public Task<ApiResult<IReadOnlyList<PayslipDto>>> ListPayslipsAsync(Guid? payPeriodId = null, Guid? employeeId = null, CancellationToken ct = default)
    {
        var qs = new List<string>();
        if (payPeriodId.HasValue) qs.Add($"payPeriodId={payPeriodId.Value}");
        if (employeeId.HasValue) qs.Add($"employeeId={employeeId.Value}");
        var path = qs.Count == 0 ? "/api/payroll/payslips" : "/api/payroll/payslips?" + string.Join("&", qs);
        return ApiClientCore.GetAsync<IReadOnlyList<PayslipDto>>(http, path, ct);
    }

    public Task<ApiResult<PayslipDto>> GetPayslipAsync(Guid id, CancellationToken ct = default) =>
        ApiClientCore.GetAsync<PayslipDto>(http, $"/api/payroll/payslips/{id}", ct);

    public Task<ApiResult<PayslipDto>> CreatePayslipAsync(CreatePayslipRequest body, CancellationToken ct = default) =>
        ApiClientCore.PostAsync<CreatePayslipRequest, PayslipDto>(http, "/api/payroll/payslips", body, ct);

    public Task<ApiResult> UpdatePayslipAsync(Guid id, UpdatePayslipRequest body, CancellationToken ct = default) =>
        ApiClientCore.PutAsync(http, $"/api/payroll/payslips/{id}", body, ct);

    public Task<ApiResult> DeletePayslipAsync(Guid id, CancellationToken ct = default) =>
        ApiClientCore.DeleteAsync(http, $"/api/payroll/payslips/{id}", ct);
}
