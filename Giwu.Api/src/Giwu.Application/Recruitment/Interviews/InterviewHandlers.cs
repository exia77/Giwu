using FluentValidation;
using Giwu.Application.Common;
using Giwu.Contracts.Recruitment;
using Giwu.Domain.Recruitment;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Giwu.Application.Recruitment.Interviews;

public sealed record ListInterviewsQuery(
    Guid? CandidateId, DateTimeOffset? From, DateTimeOffset? To, InterviewStatus? Status)
    : IRequest<Result<IReadOnlyList<InterviewDto>>>;

internal sealed class ListInterviewsHandler(IApplicationDbContext db)
    : IRequestHandler<ListInterviewsQuery, Result<IReadOnlyList<InterviewDto>>>
{
    public async Task<Result<IReadOnlyList<InterviewDto>>> Handle(ListInterviewsQuery q, CancellationToken ct)
    {
        var query = db.Interviews.AsQueryable();
        if (q.CandidateId.HasValue) query = query.Where(i => i.CandidateId == q.CandidateId.Value);
        if (q.From.HasValue) query = query.Where(i => i.ScheduledAt >= q.From.Value);
        if (q.To.HasValue) query = query.Where(i => i.ScheduledAt <= q.To.Value);
        if (q.Status.HasValue) query = query.Where(i => i.Status == q.Status.Value);

        var items = await query
            .OrderBy(i => i.ScheduledAt)
            .Select(i => new InterviewDto(
                i.Id, i.CandidateId,
                db.Candidates.Where(c => c.Id == i.CandidateId)
                    .Select(c => c.FirstName + " " + c.LastName).FirstOrDefault() ?? "",
                i.ScheduledAt, i.DurationMinutes, i.Kind, i.InterviewerEmployeeId,
                db.Employees.Where(e => e.Id == i.InterviewerEmployeeId)
                    .Select(e => e.FirstName + " " + e.LastName).FirstOrDefault(),
                i.Location, i.Status, i.Notes))
            .ToListAsync(ct);

        return Result<IReadOnlyList<InterviewDto>>.Success(items);
    }
}

public sealed record ScheduleInterviewCommand(ScheduleInterviewRequest Request)
    : IRequest<Result<InterviewDto>>;

public sealed class ScheduleInterviewValidator : AbstractValidator<ScheduleInterviewCommand>
{
    public ScheduleInterviewValidator()
    {
        RuleFor(x => x.Request.CandidateId).NotEmpty();
        RuleFor(x => x.Request.DurationMinutes).GreaterThan(0);
    }
}

internal sealed class ScheduleInterviewHandler(IApplicationDbContext db, TimeProvider clock)
    : IRequestHandler<ScheduleInterviewCommand, Result<InterviewDto>>
{
    public async Task<Result<InterviewDto>> Handle(ScheduleInterviewCommand cmd, CancellationToken ct)
    {
        var candidate = await db.Candidates.FirstOrDefaultAsync(c => c.Id == cmd.Request.CandidateId, ct);
        if (candidate is null) return Result<InterviewDto>.NotFound("Candidate not found");

        var r = cmd.Request;
        var interview = new Interview
        {
            CandidateId = r.CandidateId, ScheduledAt = r.ScheduledAt,
            DurationMinutes = r.DurationMinutes, Kind = r.Kind,
            InterviewerEmployeeId = r.InterviewerEmployeeId,
            Location = r.Location, Status = InterviewStatus.Scheduled, Notes = r.Notes,
        };
        db.Interviews.Add(interview);

        if (candidate.Stage == CandidateStage.Applied || candidate.Stage == CandidateStage.Screening)
            candidate.Stage = CandidateStage.Interview;
        candidate.LastActivityAt = clock.GetUtcNow();

        await db.SaveChangesAsync(ct);

        return Result<InterviewDto>.Success(new InterviewDto(
            interview.Id, interview.CandidateId,
            $"{candidate.FirstName} {candidate.LastName}".Trim(),
            interview.ScheduledAt, interview.DurationMinutes, interview.Kind,
            interview.InterviewerEmployeeId, null,
            interview.Location, interview.Status, interview.Notes));
    }
}

public sealed record UpdateInterviewCommand(Guid Id, UpdateInterviewRequest Request)
    : IRequest<Result>;

internal sealed class UpdateInterviewHandler(IApplicationDbContext db)
    : IRequestHandler<UpdateInterviewCommand, Result>
{
    public async Task<Result> Handle(UpdateInterviewCommand cmd, CancellationToken ct)
    {
        var i = await db.Interviews.FirstOrDefaultAsync(x => x.Id == cmd.Id, ct);
        if (i is null) return Result.NotFound();

        var r = cmd.Request;
        i.ScheduledAt = r.ScheduledAt;
        i.DurationMinutes = r.DurationMinutes;
        i.Kind = r.Kind;
        i.InterviewerEmployeeId = r.InterviewerEmployeeId;
        i.Location = r.Location;
        i.Status = r.Status;
        i.Notes = r.Notes;

        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public sealed record CancelInterviewCommand(Guid Id) : IRequest<Result>;

internal sealed class CancelInterviewHandler(IApplicationDbContext db)
    : IRequestHandler<CancelInterviewCommand, Result>
{
    public async Task<Result> Handle(CancelInterviewCommand cmd, CancellationToken ct)
    {
        var i = await db.Interviews.FirstOrDefaultAsync(x => x.Id == cmd.Id, ct);
        if (i is null) return Result.NotFound();
        i.Status = InterviewStatus.Cancelled;
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
