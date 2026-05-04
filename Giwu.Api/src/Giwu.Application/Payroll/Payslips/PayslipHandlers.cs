using FluentValidation;
using Giwu.Application.Common;
using Giwu.Contracts.Payroll;
using Giwu.Domain.Payroll;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Giwu.Application.Payroll.Payslips;

public sealed record ListPayslipsQuery(Guid? PayPeriodId, Guid? EmployeeId)
    : IRequest<Result<IReadOnlyList<PayslipDto>>>;

internal sealed class ListPayslipsHandler(IApplicationDbContext db)
    : IRequestHandler<ListPayslipsQuery, Result<IReadOnlyList<PayslipDto>>>
{
    public async Task<Result<IReadOnlyList<PayslipDto>>> Handle(ListPayslipsQuery q, CancellationToken ct)
    {
        var query = from s in db.Payslips
                    join e in db.Employees on s.EmployeeId equals e.Id
                    join d in db.Departments on e.DepartmentId equals d.Id
                    select new { s, e, DeptName = d.Name };

        if (q.PayPeriodId.HasValue) query = query.Where(x => x.s.PayPeriodId == q.PayPeriodId.Value);
        if (q.EmployeeId.HasValue) query = query.Where(x => x.s.EmployeeId == q.EmployeeId.Value);

        var rows = await query.OrderBy(x => x.e.LastName).ToListAsync(ct);

        var items = rows.Select(x => new PayslipDto(
            x.s.Id, x.s.PayPeriodId, x.s.EmployeeId,
            $"{x.e.FirstName} {x.e.LastName}".Trim(), x.e.EmployeeNumber, x.DeptName, x.e.JobTitle,
            x.s.BasicSalary, x.s.Overtime, x.s.Bonus, x.s.Allowance,
            x.s.Sss, x.s.PhilHealth, x.s.PagIbig, x.s.WithholdingTax,
            x.s.LoanDeduction, x.s.OtherDeduction,
            x.s.Gross, x.s.TotalDeductions, x.s.Net, x.s.Status, x.s.Notes)).ToList();

        return Result<IReadOnlyList<PayslipDto>>.Success(items);
    }
}

public sealed record GetPayslipQuery(Guid Id) : IRequest<Result<PayslipDto>>;

internal sealed class GetPayslipHandler(IApplicationDbContext db)
    : IRequestHandler<GetPayslipQuery, Result<PayslipDto>>
{
    public async Task<Result<PayslipDto>> Handle(GetPayslipQuery q, CancellationToken ct)
    {
        var row = await (from s in db.Payslips
                         join e in db.Employees on s.EmployeeId equals e.Id
                         join d in db.Departments on e.DepartmentId equals d.Id
                         where s.Id == q.Id
                         select new { s, e, DeptName = d.Name })
                         .FirstOrDefaultAsync(ct);
        if (row is null) return Result<PayslipDto>.NotFound();

        var x = row;
        return Result<PayslipDto>.Success(new PayslipDto(
            x.s.Id, x.s.PayPeriodId, x.s.EmployeeId,
            $"{x.e.FirstName} {x.e.LastName}".Trim(), x.e.EmployeeNumber, x.DeptName, x.e.JobTitle,
            x.s.BasicSalary, x.s.Overtime, x.s.Bonus, x.s.Allowance,
            x.s.Sss, x.s.PhilHealth, x.s.PagIbig, x.s.WithholdingTax,
            x.s.LoanDeduction, x.s.OtherDeduction,
            x.s.Gross, x.s.TotalDeductions, x.s.Net, x.s.Status, x.s.Notes));
    }
}

public sealed record CreatePayslipCommand(CreatePayslipRequest Request) : IRequest<Result<PayslipDto>>;

public sealed class CreatePayslipValidator : AbstractValidator<CreatePayslipCommand>
{
    public CreatePayslipValidator()
    {
        RuleFor(x => x.Request.PayPeriodId).NotEmpty();
        RuleFor(x => x.Request.EmployeeId).NotEmpty();
        RuleFor(x => x.Request.BasicSalary).GreaterThanOrEqualTo(0);
    }
}

internal sealed class CreatePayslipHandler(IApplicationDbContext db)
    : IRequestHandler<CreatePayslipCommand, Result<PayslipDto>>
{
    public async Task<Result<PayslipDto>> Handle(CreatePayslipCommand cmd, CancellationToken ct)
    {
        var period = await db.PayPeriods.FirstOrDefaultAsync(p => p.Id == cmd.Request.PayPeriodId, ct);
        if (period is null) return Result<PayslipDto>.NotFound("Pay period not found");
        if (period.Status == PayPeriodStatus.Released)
            return Result<PayslipDto>.Conflict("Cannot add payslips to a released period.");

        if (await db.Payslips.AnyAsync(s =>
                s.PayPeriodId == cmd.Request.PayPeriodId &&
                s.EmployeeId == cmd.Request.EmployeeId, ct))
            return Result<PayslipDto>.Conflict("Payslip already exists for this employee in this period.");

        var emp = await db.Employees.FirstOrDefaultAsync(e => e.Id == cmd.Request.EmployeeId, ct);
        if (emp is null) return Result<PayslipDto>.NotFound("Employee not found");

        var r = cmd.Request;
        var p = new Payslip
        {
            PayPeriodId = r.PayPeriodId, EmployeeId = r.EmployeeId,
            BasicSalary = r.BasicSalary, Overtime = r.Overtime, Bonus = r.Bonus, Allowance = r.Allowance,
            Sss = r.Sss, PhilHealth = r.PhilHealth, PagIbig = r.PagIbig,
            WithholdingTax = r.WithholdingTax, LoanDeduction = r.LoanDeduction, OtherDeduction = r.OtherDeduction,
            Status = PayslipStatus.Pending, Notes = r.Notes,
        };
        db.Payslips.Add(p);
        await db.SaveChangesAsync(ct);

        var dept = await db.Departments.FirstOrDefaultAsync(d => d.Id == emp.DepartmentId, ct);
        return Result<PayslipDto>.Success(new PayslipDto(
            p.Id, p.PayPeriodId, p.EmployeeId,
            $"{emp.FirstName} {emp.LastName}".Trim(), emp.EmployeeNumber,
            dept?.Name ?? "", emp.JobTitle,
            p.BasicSalary, p.Overtime, p.Bonus, p.Allowance,
            p.Sss, p.PhilHealth, p.PagIbig, p.WithholdingTax, p.LoanDeduction, p.OtherDeduction,
            p.Gross, p.TotalDeductions, p.Net, p.Status, p.Notes));
    }
}

public sealed record UpdatePayslipCommand(Guid Id, UpdatePayslipRequest Request) : IRequest<Result>;

internal sealed class UpdatePayslipHandler(IApplicationDbContext db)
    : IRequestHandler<UpdatePayslipCommand, Result>
{
    public async Task<Result> Handle(UpdatePayslipCommand cmd, CancellationToken ct)
    {
        var p = await db.Payslips.FirstOrDefaultAsync(x => x.Id == cmd.Id, ct);
        if (p is null) return Result.NotFound();

        var period = await db.PayPeriods.FirstOrDefaultAsync(x => x.Id == p.PayPeriodId, ct);
        if (period is { Status: PayPeriodStatus.Released })
            return Result.Conflict("Cannot edit payslips in a released period.");

        var r = cmd.Request;
        p.BasicSalary = r.BasicSalary;
        p.Overtime = r.Overtime;
        p.Bonus = r.Bonus;
        p.Allowance = r.Allowance;
        p.Sss = r.Sss;
        p.PhilHealth = r.PhilHealth;
        p.PagIbig = r.PagIbig;
        p.WithholdingTax = r.WithholdingTax;
        p.LoanDeduction = r.LoanDeduction;
        p.OtherDeduction = r.OtherDeduction;
        p.Notes = r.Notes;

        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public sealed record DeletePayslipCommand(Guid Id) : IRequest<Result>;

internal sealed class DeletePayslipHandler(IApplicationDbContext db)
    : IRequestHandler<DeletePayslipCommand, Result>
{
    public async Task<Result> Handle(DeletePayslipCommand cmd, CancellationToken ct)
    {
        var p = await db.Payslips.FirstOrDefaultAsync(x => x.Id == cmd.Id, ct);
        if (p is null) return Result.NotFound();

        var period = await db.PayPeriods.FirstOrDefaultAsync(x => x.Id == p.PayPeriodId, ct);
        if (period is { Status: PayPeriodStatus.Released })
            return Result.Conflict("Cannot delete payslips in a released period.");

        db.Payslips.Remove(p);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
