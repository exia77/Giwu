using FluentValidation;
using Giwu.Application.Common;
using Giwu.Contracts.Payroll;
using Giwu.Domain.Payroll;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Giwu.Application.Payroll.PayPeriods;

public sealed record ListPayPeriodsQuery(int? Year, PayPeriodStatus? Status)
    : IRequest<Result<IReadOnlyList<PayPeriodDto>>>;

internal sealed class ListPayPeriodsHandler(IApplicationDbContext db)
    : IRequestHandler<ListPayPeriodsQuery, Result<IReadOnlyList<PayPeriodDto>>>
{
    public async Task<Result<IReadOnlyList<PayPeriodDto>>> Handle(ListPayPeriodsQuery q, CancellationToken ct)
    {
        var query = db.PayPeriods.AsQueryable();
        if (q.Year.HasValue) query = query.Where(p => p.PeriodStart.Year == q.Year.Value);
        if (q.Status.HasValue) query = query.Where(p => p.Status == q.Status.Value);

        var items = await query
            .OrderByDescending(p => p.PeriodStart)
            .Select(p => new PayPeriodDto(
                p.Id, p.Code, p.PeriodStart, p.PeriodEnd, p.ReleaseDate,
                p.Frequency, p.Status, p.ApprovedById, p.ApprovedAt, p.Notes,
                db.Payslips.Count(s => s.PayPeriodId == p.Id),
                db.Payslips.Where(s => s.PayPeriodId == p.Id)
                    .Sum(s => s.BasicSalary + s.Overtime + s.Bonus + s.Allowance),
                db.Payslips.Where(s => s.PayPeriodId == p.Id)
                    .Sum(s => s.BasicSalary + s.Overtime + s.Bonus + s.Allowance
                        - s.Sss - s.PhilHealth - s.PagIbig - s.WithholdingTax
                        - s.LoanDeduction - s.OtherDeduction)))
            .ToListAsync(ct);

        return Result<IReadOnlyList<PayPeriodDto>>.Success(items);
    }
}

public sealed record GetPayPeriodQuery(Guid Id) : IRequest<Result<PayPeriodDto>>;

internal sealed class GetPayPeriodHandler(IApplicationDbContext db)
    : IRequestHandler<GetPayPeriodQuery, Result<PayPeriodDto>>
{
    public async Task<Result<PayPeriodDto>> Handle(GetPayPeriodQuery q, CancellationToken ct)
    {
        var p = await db.PayPeriods.FirstOrDefaultAsync(x => x.Id == q.Id, ct);
        if (p is null) return Result<PayPeriodDto>.NotFound();

        var slips = await db.Payslips.Where(s => s.PayPeriodId == p.Id).ToListAsync(ct);
        decimal gross = slips.Sum(s => s.Gross);
        decimal net = slips.Sum(s => s.Net);

        return Result<PayPeriodDto>.Success(new PayPeriodDto(
            p.Id, p.Code, p.PeriodStart, p.PeriodEnd, p.ReleaseDate,
            p.Frequency, p.Status, p.ApprovedById, p.ApprovedAt, p.Notes,
            slips.Count, gross, net));
    }
}

public sealed record CreatePayPeriodCommand(CreatePayPeriodRequest Request)
    : IRequest<Result<PayPeriodDto>>;

public sealed class CreatePayPeriodValidator : AbstractValidator<CreatePayPeriodCommand>
{
    public CreatePayPeriodValidator()
    {
        RuleFor(x => x.Request.Code).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Request.PeriodStart).NotEmpty();
        RuleFor(x => x.Request.PeriodEnd).GreaterThanOrEqualTo(x => x.Request.PeriodStart);
    }
}

internal sealed class CreatePayPeriodHandler(IApplicationDbContext db)
    : IRequestHandler<CreatePayPeriodCommand, Result<PayPeriodDto>>
{
    public async Task<Result<PayPeriodDto>> Handle(CreatePayPeriodCommand cmd, CancellationToken ct)
    {
        var r = cmd.Request;
        if (await db.PayPeriods.AnyAsync(p => p.Code == r.Code, ct))
            return Result<PayPeriodDto>.Conflict($"Pay period code '{r.Code}' already exists.");

        var p = new PayPeriod
        {
            Code = r.Code, PeriodStart = r.PeriodStart, PeriodEnd = r.PeriodEnd,
            ReleaseDate = r.ReleaseDate, Frequency = r.Frequency,
            Status = PayPeriodStatus.Draft, Notes = r.Notes,
        };
        db.PayPeriods.Add(p);
        await db.SaveChangesAsync(ct);

        return Result<PayPeriodDto>.Success(new PayPeriodDto(
            p.Id, p.Code, p.PeriodStart, p.PeriodEnd, p.ReleaseDate,
            p.Frequency, p.Status, null, null, p.Notes, 0, 0m, 0m));
    }
}

public sealed record UpdatePayPeriodCommand(Guid Id, UpdatePayPeriodRequest Request) : IRequest<Result>;

internal sealed class UpdatePayPeriodHandler(IApplicationDbContext db)
    : IRequestHandler<UpdatePayPeriodCommand, Result>
{
    public async Task<Result> Handle(UpdatePayPeriodCommand cmd, CancellationToken ct)
    {
        var p = await db.PayPeriods.FirstOrDefaultAsync(x => x.Id == cmd.Id, ct);
        if (p is null) return Result.NotFound();
        if (p.Status == PayPeriodStatus.Released)
            return Result.Conflict("Cannot edit a released pay period.");

        var r = cmd.Request;
        p.Code = r.Code;
        p.PeriodStart = r.PeriodStart;
        p.PeriodEnd = r.PeriodEnd;
        p.ReleaseDate = r.ReleaseDate;
        p.Frequency = r.Frequency;
        p.Notes = r.Notes;

        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public sealed record ApprovePayPeriodCommand(Guid Id, string Note) : IRequest<Result>;

internal sealed class ApprovePayPeriodHandler(IApplicationDbContext db, ICurrentUser user, TimeProvider clock)
    : IRequestHandler<ApprovePayPeriodCommand, Result>
{
    public async Task<Result> Handle(ApprovePayPeriodCommand cmd, CancellationToken ct)
    {
        var p = await db.PayPeriods.FirstOrDefaultAsync(x => x.Id == cmd.Id, ct);
        if (p is null) return Result.NotFound();
        if (p.Status == PayPeriodStatus.Approved || p.Status == PayPeriodStatus.Released)
            return Result.Conflict("Pay period already approved.");

        p.Status = PayPeriodStatus.Approved;
        p.ApprovedById = user.Id;
        p.ApprovedAt = clock.GetUtcNow();
        if (!string.IsNullOrWhiteSpace(cmd.Note))
            p.Notes = string.IsNullOrEmpty(p.Notes) ? cmd.Note : p.Notes + "\n" + cmd.Note;

        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public sealed record ReleasePayPeriodCommand(Guid Id) : IRequest<Result>;

internal sealed class ReleasePayPeriodHandler(IApplicationDbContext db)
    : IRequestHandler<ReleasePayPeriodCommand, Result>
{
    public async Task<Result> Handle(ReleasePayPeriodCommand cmd, CancellationToken ct)
    {
        var p = await db.PayPeriods.FirstOrDefaultAsync(x => x.Id == cmd.Id, ct);
        if (p is null) return Result.NotFound();
        if (p.Status != PayPeriodStatus.Approved)
            return Result.Conflict("Only approved pay periods can be released.");

        p.Status = PayPeriodStatus.Released;
        var slips = await db.Payslips.Where(s => s.PayPeriodId == p.Id).ToListAsync(ct);
        foreach (var s in slips) s.Status = PayslipStatus.Released;

        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public sealed record DeletePayPeriodCommand(Guid Id) : IRequest<Result>;

internal sealed class DeletePayPeriodHandler(IApplicationDbContext db)
    : IRequestHandler<DeletePayPeriodCommand, Result>
{
    public async Task<Result> Handle(DeletePayPeriodCommand cmd, CancellationToken ct)
    {
        var p = await db.PayPeriods.FirstOrDefaultAsync(x => x.Id == cmd.Id, ct);
        if (p is null) return Result.NotFound();
        if (p.Status != PayPeriodStatus.Draft)
            return Result.Conflict("Only draft pay periods can be deleted.");
        if (await db.Payslips.AnyAsync(s => s.PayPeriodId == p.Id, ct))
            return Result.Conflict("Pay period has payslips. Remove them first.");

        db.PayPeriods.Remove(p);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
