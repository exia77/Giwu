using FluentValidation;
using Giwu.Application.Common;
using Giwu.Contracts.Reports;
using Giwu.Domain.Reports;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Giwu.Application.Reports.Persisted;

// ── List ────────────────────────────────────────────────────────────────────
public sealed record ListReportSchedulesQuery() : IRequest<Result<IReadOnlyList<ReportScheduleDto>>>;

internal sealed class ListReportSchedulesHandler(IApplicationDbContext db)
    : IRequestHandler<ListReportSchedulesQuery, Result<IReadOnlyList<ReportScheduleDto>>>
{
    public async Task<Result<IReadOnlyList<ReportScheduleDto>>> Handle(
        ListReportSchedulesQuery q, CancellationToken ct)
    {
        var rows = await db.ReportSchedules.OrderBy(s => s.NextRunAt).ToListAsync(ct);
        return Result<IReadOnlyList<ReportScheduleDto>>.Success(rows.Select(ReportDtoMapping.ToDto).ToList());
    }
}

// ── Create ──────────────────────────────────────────────────────────────────
public sealed record CreateReportScheduleCommand(CreateReportScheduleRequest Request)
    : IRequest<Result<ReportScheduleDto>>;

public sealed class CreateReportScheduleValidator : AbstractValidator<CreateReportScheduleCommand>
{
    public CreateReportScheduleValidator()
    {
        RuleFor(x => x.Request.DefinitionId).NotEmpty();
        RuleFor(x => x.Request.DefinitionName).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Request.Cadence).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Request.Frequency).IsInEnum();
        RuleFor(x => x.Request.Format).IsInEnum();
        RuleFor(x => x.Request.Category).IsInEnum();
    }
}

internal sealed class CreateReportScheduleHandler(IApplicationDbContext db)
    : IRequestHandler<CreateReportScheduleCommand, Result<ReportScheduleDto>>
{
    public async Task<Result<ReportScheduleDto>> Handle(CreateReportScheduleCommand cmd, CancellationToken ct)
    {
        var r = cmd.Request;
        var s = new ReportSchedule
        {
            DefinitionId = r.DefinitionId,
            DefinitionName = r.DefinitionName,
            Category = r.Category,
            Frequency = r.Frequency,
            Cadence = r.Cadence,
            RecipientsCsv = string.Join(",", r.Recipients ?? Array.Empty<string>()),
            Format = r.Format,
            NextRunAt = r.NextRunAt,
            IsActive = true,
        };
        db.ReportSchedules.Add(s);
        await db.SaveChangesAsync(ct);
        return Result<ReportScheduleDto>.Success(ReportDtoMapping.ToDto(s));
    }
}

// ── Update ──────────────────────────────────────────────────────────────────
public sealed record UpdateReportScheduleCommand(Guid Id, UpdateReportScheduleRequest Request)
    : IRequest<Result>;

internal sealed class UpdateReportScheduleHandler(IApplicationDbContext db)
    : IRequestHandler<UpdateReportScheduleCommand, Result>
{
    public async Task<Result> Handle(UpdateReportScheduleCommand cmd, CancellationToken ct)
    {
        var s = await db.ReportSchedules.FirstOrDefaultAsync(x => x.Id == cmd.Id, ct);
        if (s is null) return Result.NotFound();

        var r = cmd.Request;
        s.Frequency = r.Frequency;
        s.Cadence = r.Cadence;
        s.RecipientsCsv = string.Join(",", r.Recipients ?? Array.Empty<string>());
        s.Format = r.Format;
        s.NextRunAt = r.NextRunAt;
        s.IsActive = r.IsActive;

        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ── Toggle ──────────────────────────────────────────────────────────────────
public sealed record ToggleReportScheduleCommand(Guid Id) : IRequest<Result>;

internal sealed class ToggleReportScheduleHandler(IApplicationDbContext db)
    : IRequestHandler<ToggleReportScheduleCommand, Result>
{
    public async Task<Result> Handle(ToggleReportScheduleCommand cmd, CancellationToken ct)
    {
        var s = await db.ReportSchedules.FirstOrDefaultAsync(x => x.Id == cmd.Id, ct);
        if (s is null) return Result.NotFound();

        s.IsActive = !s.IsActive;
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ── Delete ──────────────────────────────────────────────────────────────────
public sealed record DeleteReportScheduleCommand(Guid Id) : IRequest<Result>;

internal sealed class DeleteReportScheduleHandler(IApplicationDbContext db)
    : IRequestHandler<DeleteReportScheduleCommand, Result>
{
    public async Task<Result> Handle(DeleteReportScheduleCommand cmd, CancellationToken ct)
    {
        var s = await db.ReportSchedules.FirstOrDefaultAsync(x => x.Id == cmd.Id, ct);
        if (s is null) return Result.NotFound();

        db.ReportSchedules.Remove(s);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
