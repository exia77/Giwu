using FluentValidation;
using Giwu.Application.Common;
using Giwu.Contracts.Recruitment;
using Giwu.Domain.Recruitment;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Giwu.Application.Recruitment.Jobs;

// ── Queries ─────────────────────────────────────────────────────────────────
public sealed record ListJobsQuery(JobStatus? Status, Guid? DepartmentId, string? Search)
    : IRequest<Result<IReadOnlyList<JobRequisitionDto>>>;

internal sealed class ListJobsHandler(IApplicationDbContext db)
    : IRequestHandler<ListJobsQuery, Result<IReadOnlyList<JobRequisitionDto>>>
{
    public async Task<Result<IReadOnlyList<JobRequisitionDto>>> Handle(ListJobsQuery q, CancellationToken ct)
    {
        var query = from j in db.JobRequisitions
                    join d in db.Departments on j.DepartmentId equals d.Id
                    select new { j, DeptName = d.Name };

        if (q.Status.HasValue)
            query = query.Where(x => x.j.Status == q.Status.Value);
        if (q.DepartmentId.HasValue)
            query = query.Where(x => x.j.DepartmentId == q.DepartmentId.Value);
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.Trim().ToLower();
            query = query.Where(x => x.j.Title.ToLower().Contains(s));
        }

        var items = await query
            .OrderByDescending(x => x.j.PostedAt)
            .Select(x => new JobRequisitionDto(
                x.j.Id, x.j.Title, x.j.DepartmentId, x.DeptName,
                x.j.Location, x.j.EmploymentType, x.j.Openings, x.j.Filled,
                x.j.Status, x.j.OwnerEmployeeId,
                db.Employees.Where(e => e.Id == x.j.OwnerEmployeeId)
                    .Select(e => e.FirstName + " " + e.LastName).FirstOrDefault(),
                x.j.SalaryMin, x.j.SalaryMax, x.j.PostedAt, x.j.TargetFillBy, x.j.Description,
                db.Candidates.Count(c => c.JobRequisitionId == x.j.Id
                    && c.Stage != CandidateStage.Rejected && c.Stage != CandidateStage.Hired)))
            .ToListAsync(ct);

        return Result<IReadOnlyList<JobRequisitionDto>>.Success(items);
    }
}

public sealed record GetJobQuery(Guid Id) : IRequest<Result<JobRequisitionDto>>;

internal sealed class GetJobHandler(IApplicationDbContext db)
    : IRequestHandler<GetJobQuery, Result<JobRequisitionDto>>
{
    public async Task<Result<JobRequisitionDto>> Handle(GetJobQuery q, CancellationToken ct)
    {
        var dto = await (from j in db.JobRequisitions
                         join d in db.Departments on j.DepartmentId equals d.Id
                         where j.Id == q.Id
                         select new JobRequisitionDto(
                             j.Id, j.Title, j.DepartmentId, d.Name,
                             j.Location, j.EmploymentType, j.Openings, j.Filled,
                             j.Status, j.OwnerEmployeeId,
                             db.Employees.Where(e => e.Id == j.OwnerEmployeeId)
                                 .Select(e => e.FirstName + " " + e.LastName).FirstOrDefault(),
                             j.SalaryMin, j.SalaryMax, j.PostedAt, j.TargetFillBy, j.Description,
                             db.Candidates.Count(c => c.JobRequisitionId == j.Id
                                 && c.Stage != CandidateStage.Rejected && c.Stage != CandidateStage.Hired)))
                         .FirstOrDefaultAsync(ct);

        return dto is null
            ? Result<JobRequisitionDto>.NotFound()
            : Result<JobRequisitionDto>.Success(dto);
    }
}

// ── Commands ────────────────────────────────────────────────────────────────
public sealed record CreateJobCommand(CreateJobRequisitionRequest Request)
    : IRequest<Result<JobRequisitionDto>>;

public sealed class CreateJobValidator : AbstractValidator<CreateJobCommand>
{
    public CreateJobValidator()
    {
        RuleFor(x => x.Request.Title).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Request.DepartmentId).NotEmpty();
        RuleFor(x => x.Request.Openings).GreaterThan(0);
        RuleFor(x => x.Request.SalaryMin).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Request.SalaryMax).GreaterThanOrEqualTo(x => x.Request.SalaryMin);
    }
}

internal sealed class CreateJobHandler(IApplicationDbContext db, TimeProvider clock)
    : IRequestHandler<CreateJobCommand, Result<JobRequisitionDto>>
{
    public async Task<Result<JobRequisitionDto>> Handle(CreateJobCommand cmd, CancellationToken ct)
    {
        var dept = await db.Departments.FirstOrDefaultAsync(d => d.Id == cmd.Request.DepartmentId, ct);
        if (dept is null) return Result<JobRequisitionDto>.NotFound("Department not found");

        var r = cmd.Request;
        var job = new JobRequisition
        {
            Title = r.Title, DepartmentId = r.DepartmentId, Location = r.Location,
            EmploymentType = r.EmploymentType, Openings = r.Openings, Filled = 0,
            Status = r.Status, OwnerEmployeeId = r.OwnerEmployeeId,
            SalaryMin = r.SalaryMin, SalaryMax = r.SalaryMax,
            PostedAt = clock.GetUtcNow(),
            TargetFillBy = r.TargetFillBy, Description = r.Description,
        };
        db.JobRequisitions.Add(job);
        await db.SaveChangesAsync(ct);

        return Result<JobRequisitionDto>.Success(new JobRequisitionDto(
            job.Id, job.Title, job.DepartmentId, dept.Name,
            job.Location, job.EmploymentType, job.Openings, job.Filled,
            job.Status, job.OwnerEmployeeId, null,
            job.SalaryMin, job.SalaryMax, job.PostedAt, job.TargetFillBy, job.Description, 0));
    }
}

public sealed record UpdateJobCommand(Guid Id, UpdateJobRequisitionRequest Request)
    : IRequest<Result<JobRequisitionDto>>;

public sealed class UpdateJobValidator : AbstractValidator<UpdateJobCommand>
{
    public UpdateJobValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Request.Title).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Request.DepartmentId).NotEmpty();
        RuleFor(x => x.Request.Openings).GreaterThan(0);
        RuleFor(x => x.Request.Filled).GreaterThanOrEqualTo(0);
    }
}

internal sealed class UpdateJobHandler(IApplicationDbContext db)
    : IRequestHandler<UpdateJobCommand, Result<JobRequisitionDto>>
{
    public async Task<Result<JobRequisitionDto>> Handle(UpdateJobCommand cmd, CancellationToken ct)
    {
        var job = await db.JobRequisitions.FirstOrDefaultAsync(x => x.Id == cmd.Id, ct);
        if (job is null) return Result<JobRequisitionDto>.NotFound();

        var dept = await db.Departments.FirstOrDefaultAsync(d => d.Id == cmd.Request.DepartmentId, ct);
        if (dept is null) return Result<JobRequisitionDto>.NotFound("Department not found");

        var r = cmd.Request;
        job.Title = r.Title;
        job.DepartmentId = r.DepartmentId;
        job.Location = r.Location;
        job.EmploymentType = r.EmploymentType;
        job.Openings = r.Openings;
        job.Filled = r.Filled;
        job.OwnerEmployeeId = r.OwnerEmployeeId;
        job.SalaryMin = r.SalaryMin;
        job.SalaryMax = r.SalaryMax;
        job.TargetFillBy = r.TargetFillBy;
        job.Description = r.Description;

        await db.SaveChangesAsync(ct);

        return Result<JobRequisitionDto>.Success(new JobRequisitionDto(
            job.Id, job.Title, job.DepartmentId, dept.Name,
            job.Location, job.EmploymentType, job.Openings, job.Filled,
            job.Status, job.OwnerEmployeeId, null,
            job.SalaryMin, job.SalaryMax, job.PostedAt, job.TargetFillBy, job.Description, 0));
    }
}

public sealed record ChangeJobStatusCommand(Guid Id, JobStatus Status) : IRequest<Result>;

internal sealed class ChangeJobStatusHandler(IApplicationDbContext db)
    : IRequestHandler<ChangeJobStatusCommand, Result>
{
    public async Task<Result> Handle(ChangeJobStatusCommand cmd, CancellationToken ct)
    {
        var job = await db.JobRequisitions.FirstOrDefaultAsync(x => x.Id == cmd.Id, ct);
        if (job is null) return Result.NotFound();
        job.Status = cmd.Status;
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public sealed record DeleteJobCommand(Guid Id) : IRequest<Result>;

internal sealed class DeleteJobHandler(IApplicationDbContext db)
    : IRequestHandler<DeleteJobCommand, Result>
{
    public async Task<Result> Handle(DeleteJobCommand cmd, CancellationToken ct)
    {
        var job = await db.JobRequisitions.FirstOrDefaultAsync(x => x.Id == cmd.Id, ct);
        if (job is null) return Result.NotFound();
        db.JobRequisitions.Remove(job);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
