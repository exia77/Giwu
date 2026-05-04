using FluentValidation;
using Giwu.Application.Common;
using Giwu.Contracts.Recruitment;
using Giwu.Domain.Recruitment;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Giwu.Application.Recruitment.Candidates;

public sealed record ListCandidatesQuery(
    CandidateStage? Stage, Guid? JobRequisitionId, string? Source, int? MinRating, string? Search)
    : IRequest<Result<IReadOnlyList<CandidateDto>>>;

internal sealed class ListCandidatesHandler(IApplicationDbContext db)
    : IRequestHandler<ListCandidatesQuery, Result<IReadOnlyList<CandidateDto>>>
{
    public async Task<Result<IReadOnlyList<CandidateDto>>> Handle(ListCandidatesQuery q, CancellationToken ct)
    {
        var query = from c in db.Candidates
                    join j in db.JobRequisitions on c.JobRequisitionId equals j.Id
                    select new { c, JobTitle = j.Title };

        if (q.Stage.HasValue) query = query.Where(x => x.c.Stage == q.Stage.Value);
        if (q.JobRequisitionId.HasValue) query = query.Where(x => x.c.JobRequisitionId == q.JobRequisitionId.Value);
        if (!string.IsNullOrWhiteSpace(q.Source)) query = query.Where(x => x.c.Source == q.Source);
        if (q.MinRating.HasValue) query = query.Where(x => x.c.Rating >= q.MinRating.Value);
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.Trim().ToLower();
            query = query.Where(x =>
                x.c.FirstName.ToLower().Contains(s) ||
                x.c.LastName.ToLower().Contains(s) ||
                x.c.Email.ToLower().Contains(s));
        }

        var items = await query
            .OrderByDescending(x => x.c.LastActivityAt)
            .Select(x => new CandidateDto(
                x.c.Id, x.c.JobRequisitionId, x.JobTitle,
                x.c.FirstName, x.c.LastName, x.c.Email, x.c.Phone,
                x.c.Stage, x.c.Source, x.c.Rating,
                x.c.AppliedAt, x.c.LastActivityAt, x.c.Notes, x.c.RejectionReason))
            .ToListAsync(ct);

        return Result<IReadOnlyList<CandidateDto>>.Success(items);
    }
}

public sealed record GetCandidateQuery(Guid Id) : IRequest<Result<CandidateDto>>;

internal sealed class GetCandidateHandler(IApplicationDbContext db)
    : IRequestHandler<GetCandidateQuery, Result<CandidateDto>>
{
    public async Task<Result<CandidateDto>> Handle(GetCandidateQuery q, CancellationToken ct)
    {
        var dto = await (from c in db.Candidates
                         join j in db.JobRequisitions on c.JobRequisitionId equals j.Id
                         where c.Id == q.Id
                         select new CandidateDto(
                             c.Id, c.JobRequisitionId, j.Title,
                             c.FirstName, c.LastName, c.Email, c.Phone,
                             c.Stage, c.Source, c.Rating,
                             c.AppliedAt, c.LastActivityAt, c.Notes, c.RejectionReason))
                         .FirstOrDefaultAsync(ct);
        return dto is null ? Result<CandidateDto>.NotFound() : Result<CandidateDto>.Success(dto);
    }
}

public sealed record CreateCandidateCommand(CreateCandidateRequest Request)
    : IRequest<Result<CandidateDto>>;

public sealed class CreateCandidateValidator : AbstractValidator<CreateCandidateCommand>
{
    public CreateCandidateValidator()
    {
        RuleFor(x => x.Request.JobRequisitionId).NotEmpty();
        RuleFor(x => x.Request.FirstName).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Request.LastName).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Request.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Request.Rating).InclusiveBetween(0, 5);
    }
}

internal sealed class CreateCandidateHandler(IApplicationDbContext db, TimeProvider clock)
    : IRequestHandler<CreateCandidateCommand, Result<CandidateDto>>
{
    public async Task<Result<CandidateDto>> Handle(CreateCandidateCommand cmd, CancellationToken ct)
    {
        var job = await db.JobRequisitions.FirstOrDefaultAsync(j => j.Id == cmd.Request.JobRequisitionId, ct);
        if (job is null) return Result<CandidateDto>.NotFound("Job requisition not found");

        var r = cmd.Request;
        var now = clock.GetUtcNow();
        var candidate = new Candidate
        {
            JobRequisitionId = r.JobRequisitionId,
            FirstName = r.FirstName, LastName = r.LastName,
            Email = r.Email, Phone = r.Phone,
            Stage = CandidateStage.Applied, Source = r.Source,
            Rating = r.Rating, Notes = r.Notes,
            AppliedAt = now, LastActivityAt = now,
        };
        db.Candidates.Add(candidate);
        await db.SaveChangesAsync(ct);

        return Result<CandidateDto>.Success(new CandidateDto(
            candidate.Id, candidate.JobRequisitionId, job.Title,
            candidate.FirstName, candidate.LastName, candidate.Email, candidate.Phone,
            candidate.Stage, candidate.Source, candidate.Rating,
            candidate.AppliedAt, candidate.LastActivityAt, candidate.Notes, null));
    }
}

public sealed record UpdateCandidateCommand(Guid Id, UpdateCandidateRequest Request)
    : IRequest<Result>;

public sealed class UpdateCandidateValidator : AbstractValidator<UpdateCandidateCommand>
{
    public UpdateCandidateValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Request.FirstName).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Request.LastName).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Request.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Request.Rating).InclusiveBetween(0, 5);
    }
}

internal sealed class UpdateCandidateHandler(IApplicationDbContext db, TimeProvider clock)
    : IRequestHandler<UpdateCandidateCommand, Result>
{
    public async Task<Result> Handle(UpdateCandidateCommand cmd, CancellationToken ct)
    {
        var c = await db.Candidates.FirstOrDefaultAsync(x => x.Id == cmd.Id, ct);
        if (c is null) return Result.NotFound();

        var r = cmd.Request;
        c.FirstName = r.FirstName;
        c.LastName = r.LastName;
        c.Email = r.Email;
        c.Phone = r.Phone;
        c.Source = r.Source;
        c.Rating = r.Rating;
        c.Notes = r.Notes;
        c.LastActivityAt = clock.GetUtcNow();

        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public sealed record AdvanceCandidateCommand(Guid Id, CandidateStage NewStage, string? Note)
    : IRequest<Result>;

internal sealed class AdvanceCandidateHandler(IApplicationDbContext db, TimeProvider clock)
    : IRequestHandler<AdvanceCandidateCommand, Result>
{
    public async Task<Result> Handle(AdvanceCandidateCommand cmd, CancellationToken ct)
    {
        var c = await db.Candidates.FirstOrDefaultAsync(x => x.Id == cmd.Id, ct);
        if (c is null) return Result.NotFound();

        c.Stage = cmd.NewStage;
        c.LastActivityAt = clock.GetUtcNow();
        if (!string.IsNullOrWhiteSpace(cmd.Note))
            c.Notes = string.IsNullOrEmpty(c.Notes) ? cmd.Note : c.Notes + "\n" + cmd.Note;

        if (cmd.NewStage == CandidateStage.Hired)
        {
            var job = await db.JobRequisitions.FirstOrDefaultAsync(j => j.Id == c.JobRequisitionId, ct);
            if (job is not null) job.Filled++;
        }

        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public sealed record RejectCandidateCommand(Guid Id, string Reason) : IRequest<Result>;

internal sealed class RejectCandidateHandler(IApplicationDbContext db, TimeProvider clock)
    : IRequestHandler<RejectCandidateCommand, Result>
{
    public async Task<Result> Handle(RejectCandidateCommand cmd, CancellationToken ct)
    {
        var c = await db.Candidates.FirstOrDefaultAsync(x => x.Id == cmd.Id, ct);
        if (c is null) return Result.NotFound();
        c.Stage = CandidateStage.Rejected;
        c.RejectionReason = cmd.Reason;
        c.LastActivityAt = clock.GetUtcNow();
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public sealed record DeleteCandidateCommand(Guid Id) : IRequest<Result>;

internal sealed class DeleteCandidateHandler(IApplicationDbContext db)
    : IRequestHandler<DeleteCandidateCommand, Result>
{
    public async Task<Result> Handle(DeleteCandidateCommand cmd, CancellationToken ct)
    {
        var c = await db.Candidates.FirstOrDefaultAsync(x => x.Id == cmd.Id, ct);
        if (c is null) return Result.NotFound();
        db.Candidates.Remove(c);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
