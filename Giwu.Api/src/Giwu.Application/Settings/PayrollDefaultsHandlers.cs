using FluentValidation;
using Giwu.Application.Common;
using Giwu.Contracts.Settings;
using Giwu.Domain.Tenancy;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Giwu.Application.Settings;

// ── Get ─────────────────────────────────────────────────────────────────────
public sealed record GetPayrollDefaultsQuery : IRequest<Result<PayrollDefaultsDto>>;

internal sealed class GetPayrollDefaultsHandler(IApplicationDbContext db, ICurrentUser user)
    : IRequestHandler<GetPayrollDefaultsQuery, Result<PayrollDefaultsDto>>
{
    public async Task<Result<PayrollDefaultsDto>> Handle(GetPayrollDefaultsQuery _, CancellationToken ct)
    {
        if (!user.IsAuthenticated) return Result<PayrollDefaultsDto>.Forbidden();

        var t = await db.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == user.TenantId && x.DeletedAt == null, ct);

        if (t is null) return Result<PayrollDefaultsDto>.NotFound();

        var p = t.Payroll ?? new PayrollDefaults();
        return Result<PayrollDefaultsDto>.Success(PayrollDefaultsMapper.ToDto(p));
    }
}

// ── Update ──────────────────────────────────────────────────────────────────
public sealed record UpdatePayrollDefaultsCommand(UpdatePayrollDefaultsRequest Request)
    : IRequest<Result<PayrollDefaultsDto>>;

public sealed class UpdatePayrollDefaultsValidator : AbstractValidator<UpdatePayrollDefaultsCommand>
{
    public UpdatePayrollDefaultsValidator()
    {
        RuleFor(x => x.Request.PayFrequency).IsInEnum();
        RuleFor(x => x.Request.FirstCutoffDay).InclusiveBetween(1, 28);
        RuleFor(x => x.Request.SecondCutoffDay).InclusiveBetween(1, 31);
        RuleFor(x => x.Request.RegularOvertimeRate).InclusiveBetween(1m, 5m);
        RuleFor(x => x.Request.RestDayOvertimeRate).InclusiveBetween(1m, 5m);
        RuleFor(x => x.Request.HolidayOvertimeRate).InclusiveBetween(1m, 5m);
        RuleFor(x => x.Request.NightDifferentialRate).InclusiveBetween(1m, 5m);
    }
}

internal sealed class UpdatePayrollDefaultsHandler(IApplicationDbContext db, ICurrentUser user)
    : IRequestHandler<UpdatePayrollDefaultsCommand, Result<PayrollDefaultsDto>>
{
    public async Task<Result<PayrollDefaultsDto>> Handle(UpdatePayrollDefaultsCommand cmd, CancellationToken ct)
    {
        if (!user.IsAuthenticated) return Result<PayrollDefaultsDto>.Forbidden();

        var t = await db.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == user.TenantId && x.DeletedAt == null, ct);

        if (t is null) return Result<PayrollDefaultsDto>.NotFound();

        t.Payroll ??= new PayrollDefaults();
        var p = t.Payroll;
        var r = cmd.Request;
        p.PayFrequency                = r.PayFrequency;
        p.FirstCutoffDay              = r.FirstCutoffDay;
        p.SecondCutoffDay             = r.SecondCutoffDay;
        p.RegularOvertimeRate         = r.RegularOvertimeRate;
        p.RestDayOvertimeRate         = r.RestDayOvertimeRate;
        p.HolidayOvertimeRate         = r.HolidayOvertimeRate;
        p.NightDifferentialRate       = r.NightDifferentialRate;
        p.IncludeAllowanceIn13thMonth = r.IncludeAllowanceIn13thMonth;
        p.IncludeOtIn13thMonth        = r.IncludeOtIn13thMonth;
        p.RoundStatutoryDeductions    = r.RoundStatutoryDeductions;

        await db.SaveChangesAsync(ct);
        return Result<PayrollDefaultsDto>.Success(PayrollDefaultsMapper.ToDto(p));
    }
}

internal static class PayrollDefaultsMapper
{
    public static PayrollDefaultsDto ToDto(PayrollDefaults p) => new(
        p.PayFrequency, p.FirstCutoffDay, p.SecondCutoffDay,
        p.RegularOvertimeRate, p.RestDayOvertimeRate, p.HolidayOvertimeRate,
        p.NightDifferentialRate,
        p.IncludeAllowanceIn13thMonth, p.IncludeOtIn13thMonth,
        p.RoundStatutoryDeductions);
}
