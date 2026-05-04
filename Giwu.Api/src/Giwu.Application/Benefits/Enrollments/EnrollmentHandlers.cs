using FluentValidation;
using Giwu.Application.Common;
using Giwu.Contracts.Benefits;
using Giwu.Domain.Benefits;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Giwu.Application.Benefits.Enrollments;

public sealed record ListEnrollmentsQuery(Guid? EmployeeId, Guid? BenefitProgramId, EnrollmentStatus? Status)
    : IRequest<Result<IReadOnlyList<BenefitEnrollmentDto>>>;

internal sealed class ListEnrollmentsHandler(IApplicationDbContext db)
    : IRequestHandler<ListEnrollmentsQuery, Result<IReadOnlyList<BenefitEnrollmentDto>>>
{
    public async Task<Result<IReadOnlyList<BenefitEnrollmentDto>>> Handle(ListEnrollmentsQuery q, CancellationToken ct)
    {
        var query = from en in db.BenefitEnrollments
                    join e in db.Employees on en.EmployeeId equals e.Id
                    join p in db.BenefitPrograms on en.BenefitProgramId equals p.Id
                    select new { en, e, ProgramName = p.Name };

        if (q.EmployeeId.HasValue) query = query.Where(x => x.en.EmployeeId == q.EmployeeId.Value);
        if (q.BenefitProgramId.HasValue) query = query.Where(x => x.en.BenefitProgramId == q.BenefitProgramId.Value);
        if (q.Status.HasValue) query = query.Where(x => x.en.Status == q.Status.Value);

        var items = await query
            .OrderByDescending(x => x.en.EnrolledOn)
            .Select(x => new BenefitEnrollmentDto(
                x.en.Id, x.en.EmployeeId, x.e.FirstName + " " + x.e.LastName,
                x.en.BenefitProgramId, x.ProgramName,
                x.en.EnrolledOn, x.en.EndDate, x.en.Status,
                x.en.MonthlyContribution, x.en.Notes))
            .ToListAsync(ct);

        return Result<IReadOnlyList<BenefitEnrollmentDto>>.Success(items);
    }
}

public sealed record CreateEnrollmentCommand(CreateBenefitEnrollmentRequest Request)
    : IRequest<Result<BenefitEnrollmentDto>>;

public sealed class CreateEnrollmentValidator : AbstractValidator<CreateEnrollmentCommand>
{
    public CreateEnrollmentValidator()
    {
        RuleFor(x => x.Request.EmployeeId).NotEmpty();
        RuleFor(x => x.Request.BenefitProgramId).NotEmpty();
        RuleFor(x => x.Request.MonthlyContribution).GreaterThanOrEqualTo(0);
    }
}

internal sealed class CreateEnrollmentHandler(IApplicationDbContext db)
    : IRequestHandler<CreateEnrollmentCommand, Result<BenefitEnrollmentDto>>
{
    public async Task<Result<BenefitEnrollmentDto>> Handle(CreateEnrollmentCommand cmd, CancellationToken ct)
    {
        var emp = await db.Employees.FirstOrDefaultAsync(e => e.Id == cmd.Request.EmployeeId, ct);
        if (emp is null) return Result<BenefitEnrollmentDto>.NotFound("Employee not found");

        var program = await db.BenefitPrograms.FirstOrDefaultAsync(p => p.Id == cmd.Request.BenefitProgramId, ct);
        if (program is null) return Result<BenefitEnrollmentDto>.NotFound("Benefit program not found");

        if (await db.BenefitEnrollments.AnyAsync(en =>
                en.EmployeeId == cmd.Request.EmployeeId &&
                en.BenefitProgramId == cmd.Request.BenefitProgramId &&
                en.Status == EnrollmentStatus.Active, ct))
            return Result<BenefitEnrollmentDto>.Conflict("Employee is already actively enrolled in this program.");

        var r = cmd.Request;
        var en = new BenefitEnrollment
        {
            EmployeeId = r.EmployeeId, BenefitProgramId = r.BenefitProgramId,
            EnrolledOn = r.EnrolledOn, Status = EnrollmentStatus.Active,
            MonthlyContribution = r.MonthlyContribution, Notes = r.Notes,
        };
        db.BenefitEnrollments.Add(en);
        await db.SaveChangesAsync(ct);

        return Result<BenefitEnrollmentDto>.Success(new BenefitEnrollmentDto(
            en.Id, en.EmployeeId, $"{emp.FirstName} {emp.LastName}".Trim(),
            en.BenefitProgramId, program.Name,
            en.EnrolledOn, en.EndDate, en.Status, en.MonthlyContribution, en.Notes));
    }
}

public sealed record UpdateEnrollmentCommand(Guid Id, UpdateBenefitEnrollmentRequest Request) : IRequest<Result>;

internal sealed class UpdateEnrollmentHandler(IApplicationDbContext db)
    : IRequestHandler<UpdateEnrollmentCommand, Result>
{
    public async Task<Result> Handle(UpdateEnrollmentCommand cmd, CancellationToken ct)
    {
        var en = await db.BenefitEnrollments.FirstOrDefaultAsync(x => x.Id == cmd.Id, ct);
        if (en is null) return Result.NotFound();

        var r = cmd.Request;
        en.EnrolledOn = r.EnrolledOn;
        en.EndDate = r.EndDate;
        en.Status = r.Status;
        en.MonthlyContribution = r.MonthlyContribution;
        en.Notes = r.Notes;

        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public sealed record DeleteEnrollmentCommand(Guid Id) : IRequest<Result>;

internal sealed class DeleteEnrollmentHandler(IApplicationDbContext db)
    : IRequestHandler<DeleteEnrollmentCommand, Result>
{
    public async Task<Result> Handle(DeleteEnrollmentCommand cmd, CancellationToken ct)
    {
        var en = await db.BenefitEnrollments.FirstOrDefaultAsync(x => x.Id == cmd.Id, ct);
        if (en is null) return Result.NotFound();
        db.BenefitEnrollments.Remove(en);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ── Benefit Requests (claims/loans) ─────────────────────────────────────────
public sealed record ListBenefitRequestsQuery(Guid? EmployeeId, BenefitRequestStatus? Status, BenefitRequestType? Type)
    : IRequest<Result<IReadOnlyList<BenefitRequestDto>>>;

internal sealed class ListBenefitRequestsHandler(IApplicationDbContext db)
    : IRequestHandler<ListBenefitRequestsQuery, Result<IReadOnlyList<BenefitRequestDto>>>
{
    public async Task<Result<IReadOnlyList<BenefitRequestDto>>> Handle(ListBenefitRequestsQuery q, CancellationToken ct)
    {
        var query = from r in db.BenefitRequests
                    join e in db.Employees on r.EmployeeId equals e.Id
                    join p in db.BenefitPrograms on r.BenefitProgramId equals p.Id
                    select new { r, e, ProgramName = p.Name };

        if (q.EmployeeId.HasValue) query = query.Where(x => x.r.EmployeeId == q.EmployeeId.Value);
        if (q.Status.HasValue) query = query.Where(x => x.r.Status == q.Status.Value);
        if (q.Type.HasValue) query = query.Where(x => x.r.Type == q.Type.Value);

        var items = await query
            .OrderByDescending(x => x.r.RequestedAt)
            .Select(x => new BenefitRequestDto(
                x.r.Id, x.r.EmployeeId, x.e.FirstName + " " + x.e.LastName,
                x.r.BenefitProgramId, x.ProgramName,
                x.r.Type, x.r.Amount, x.r.RequestedAt, x.r.ResolvedAt, x.r.Status, x.r.Reason,
                x.r.TermMonths, x.r.MonthlyDeduction, x.r.OutstandingBalance))
            .ToListAsync(ct);

        return Result<IReadOnlyList<BenefitRequestDto>>.Success(items);
    }
}

public sealed record FileBenefitRequestCommand(FileBenefitRequestRequest Request)
    : IRequest<Result<BenefitRequestDto>>;

public sealed class FileBenefitRequestValidator : AbstractValidator<FileBenefitRequestCommand>
{
    public FileBenefitRequestValidator()
    {
        RuleFor(x => x.Request.EmployeeId).NotEmpty();
        RuleFor(x => x.Request.BenefitProgramId).NotEmpty();
        RuleFor(x => x.Request.Amount).GreaterThan(0);
    }
}

internal sealed class FileBenefitRequestHandler(IApplicationDbContext db, TimeProvider clock)
    : IRequestHandler<FileBenefitRequestCommand, Result<BenefitRequestDto>>
{
    public async Task<Result<BenefitRequestDto>> Handle(FileBenefitRequestCommand cmd, CancellationToken ct)
    {
        var emp = await db.Employees.FirstOrDefaultAsync(e => e.Id == cmd.Request.EmployeeId, ct);
        if (emp is null) return Result<BenefitRequestDto>.NotFound("Employee not found");
        var program = await db.BenefitPrograms.FirstOrDefaultAsync(p => p.Id == cmd.Request.BenefitProgramId, ct);
        if (program is null) return Result<BenefitRequestDto>.NotFound("Benefit program not found");

        var r = cmd.Request;
        var req = new BenefitRequest
        {
            EmployeeId = r.EmployeeId, BenefitProgramId = r.BenefitProgramId,
            Type = r.Type, Amount = r.Amount, Reason = r.Reason,
            TermMonths = r.TermMonths, MonthlyDeduction = r.MonthlyDeduction,
            OutstandingBalance = r.Type == BenefitRequestType.Loan ? r.Amount : null,
            RequestedAt = clock.GetUtcNow(), Status = BenefitRequestStatus.Pending,
        };
        db.BenefitRequests.Add(req);
        await db.SaveChangesAsync(ct);

        return Result<BenefitRequestDto>.Success(new BenefitRequestDto(
            req.Id, req.EmployeeId, $"{emp.FirstName} {emp.LastName}".Trim(),
            req.BenefitProgramId, program.Name,
            req.Type, req.Amount, req.RequestedAt, null, req.Status, req.Reason,
            req.TermMonths, req.MonthlyDeduction, req.OutstandingBalance));
    }
}

public sealed record ResolveBenefitRequestCommand(Guid Id, BenefitRequestStatus Status, string Note)
    : IRequest<Result>;

internal sealed class ResolveBenefitRequestHandler(IApplicationDbContext db, TimeProvider clock)
    : IRequestHandler<ResolveBenefitRequestCommand, Result>
{
    public async Task<Result> Handle(ResolveBenefitRequestCommand cmd, CancellationToken ct)
    {
        var r = await db.BenefitRequests.FirstOrDefaultAsync(x => x.Id == cmd.Id, ct);
        if (r is null) return Result.NotFound();
        r.Status = cmd.Status;
        r.ResolvedAt = clock.GetUtcNow();
        if (!string.IsNullOrWhiteSpace(cmd.Note))
            r.Reason = string.IsNullOrEmpty(r.Reason) ? cmd.Note : r.Reason + "\n" + cmd.Note;
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
