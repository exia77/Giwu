using FluentValidation;
using Giwu.Application.Common;
using Giwu.Contracts.Reports;
using Giwu.Domain.Reports;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Giwu.Application.Reports.Persisted;

// ── List ────────────────────────────────────────────────────────────────────
public sealed record ListComplianceDeadlinesQuery()
    : IRequest<Result<IReadOnlyList<ComplianceDeadlineDto>>>;

internal sealed class ListComplianceDeadlinesHandler(IApplicationDbContext db)
    : IRequestHandler<ListComplianceDeadlinesQuery, Result<IReadOnlyList<ComplianceDeadlineDto>>>
{
    public async Task<Result<IReadOnlyList<ComplianceDeadlineDto>>> Handle(
        ListComplianceDeadlinesQuery q, CancellationToken ct)
    {
        var rows = await db.ComplianceDeadlines.OrderBy(c => c.DueDate).ToListAsync(ct);
        return Result<IReadOnlyList<ComplianceDeadlineDto>>.Success(rows.Select(ReportDtoMapping.ToDto).ToList());
    }
}

// ── Create ──────────────────────────────────────────────────────────────────
public sealed record CreateComplianceDeadlineCommand(CreateComplianceDeadlineRequest Request)
    : IRequest<Result<ComplianceDeadlineDto>>;

public sealed class CreateComplianceDeadlineValidator : AbstractValidator<CreateComplianceDeadlineCommand>
{
    public CreateComplianceDeadlineValidator()
    {
        RuleFor(x => x.Request.Name).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Request.Agency).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Request.FormCode).NotEmpty().MaximumLength(32);
    }
}

internal sealed class CreateComplianceDeadlineHandler(IApplicationDbContext db)
    : IRequestHandler<CreateComplianceDeadlineCommand, Result<ComplianceDeadlineDto>>
{
    public async Task<Result<ComplianceDeadlineDto>> Handle(
        CreateComplianceDeadlineCommand cmd, CancellationToken ct)
    {
        var r = cmd.Request;
        var c = new ComplianceDeadline
        {
            Agency = r.Agency,
            FormCode = r.FormCode,
            Name = r.Name,
            Description = r.Description,
            DueDate = r.DueDate,
            PeriodCovered = r.PeriodCovered,
            RelatedReportCode = r.RelatedReportCode,
        };
        db.ComplianceDeadlines.Add(c);
        await db.SaveChangesAsync(ct);
        return Result<ComplianceDeadlineDto>.Success(ReportDtoMapping.ToDto(c));
    }
}

// ── Update ──────────────────────────────────────────────────────────────────
public sealed record UpdateComplianceDeadlineCommand(Guid Id, UpdateComplianceDeadlineRequest Request)
    : IRequest<Result>;

internal sealed class UpdateComplianceDeadlineHandler(IApplicationDbContext db)
    : IRequestHandler<UpdateComplianceDeadlineCommand, Result>
{
    public async Task<Result> Handle(UpdateComplianceDeadlineCommand cmd, CancellationToken ct)
    {
        var c = await db.ComplianceDeadlines.FirstOrDefaultAsync(x => x.Id == cmd.Id, ct);
        if (c is null) return Result.NotFound();

        var r = cmd.Request;
        c.Agency = r.Agency;
        c.FormCode = r.FormCode;
        c.Name = r.Name;
        c.Description = r.Description;
        c.DueDate = r.DueDate;
        c.PeriodCovered = r.PeriodCovered;
        c.RelatedReportCode = r.RelatedReportCode;

        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ── Mark filed / unfile ─────────────────────────────────────────────────────
public sealed record SetComplianceFiledCommand(Guid Id, bool Filed) : IRequest<Result>;

internal sealed class SetComplianceFiledHandler(IApplicationDbContext db, TimeProvider clock)
    : IRequestHandler<SetComplianceFiledCommand, Result>
{
    public async Task<Result> Handle(SetComplianceFiledCommand cmd, CancellationToken ct)
    {
        var c = await db.ComplianceDeadlines.FirstOrDefaultAsync(x => x.Id == cmd.Id, ct);
        if (c is null) return Result.NotFound();

        c.IsFiled = cmd.Filed;
        c.FiledOn = cmd.Filed ? DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime) : null;

        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
