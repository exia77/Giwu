using FluentValidation;
using Giwu.Application.Common;
using Giwu.Contracts.Reports;
using Giwu.Domain.Reports;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Giwu.Application.Reports.Persisted;

// ── List ────────────────────────────────────────────────────────────────────
public sealed record ListReportDefinitionsQuery() : IRequest<Result<IReadOnlyList<ReportDefinitionDto>>>;

internal sealed class ListReportDefinitionsHandler(IApplicationDbContext db)
    : IRequestHandler<ListReportDefinitionsQuery, Result<IReadOnlyList<ReportDefinitionDto>>>
{
    public async Task<Result<IReadOnlyList<ReportDefinitionDto>>> Handle(
        ListReportDefinitionsQuery q, CancellationToken ct)
    {
        var rows = await db.ReportDefinitions
            .OrderBy(d => d.Name)
            .ToListAsync(ct);
        var items = rows.Select(ReportDtoMapping.ToDto).ToList();
        return Result<IReadOnlyList<ReportDefinitionDto>>.Success(items);
    }
}

// ── Create ──────────────────────────────────────────────────────────────────
public sealed record CreateReportDefinitionCommand(CreateReportDefinitionRequest Request)
    : IRequest<Result<ReportDefinitionDto>>;

public sealed class CreateReportDefinitionValidator : AbstractValidator<CreateReportDefinitionCommand>
{
    public CreateReportDefinitionValidator()
    {
        RuleFor(x => x.Request.Name).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Request.Category).IsInEnum();
    }
}

internal sealed class CreateReportDefinitionHandler(IApplicationDbContext db)
    : IRequestHandler<CreateReportDefinitionCommand, Result<ReportDefinitionDto>>
{
    public async Task<Result<ReportDefinitionDto>> Handle(
        CreateReportDefinitionCommand cmd, CancellationToken ct)
    {
        var r = cmd.Request;
        var nextSeq = await db.ReportDefinitions.CountAsync(ct) + 1;

        var def = new ReportDefinition
        {
            Code = $"RD-CUSTOM-{nextSeq:000}",
            Name = r.Name,
            ShortDescription = r.ShortDescription,
            LongDescription = r.LongDescription,
            Category = r.Category,
            RequiresDateRange = r.RequiresDateRange,
            RequiresDepartmentFilter = r.RequiresDepartmentFilter,
            SupportedFormatsCsv = ReportDtoMapping.JoinFormats(r.SupportedFormats),
            ColumnsCsv = string.Join("|", r.Columns ?? Array.Empty<string>()),
        };

        db.ReportDefinitions.Add(def);
        await db.SaveChangesAsync(ct);
        return Result<ReportDefinitionDto>.Success(ReportDtoMapping.ToDto(def));
    }
}

// ── Update ──────────────────────────────────────────────────────────────────
public sealed record UpdateReportDefinitionCommand(Guid Id, UpdateReportDefinitionRequest Request)
    : IRequest<Result>;

internal sealed class UpdateReportDefinitionHandler(IApplicationDbContext db)
    : IRequestHandler<UpdateReportDefinitionCommand, Result>
{
    public async Task<Result> Handle(UpdateReportDefinitionCommand cmd, CancellationToken ct)
    {
        var def = await db.ReportDefinitions.FirstOrDefaultAsync(x => x.Id == cmd.Id, ct);
        if (def is null) return Result.NotFound();

        var r = cmd.Request;
        def.Name = r.Name;
        def.ShortDescription = r.ShortDescription;
        def.LongDescription = r.LongDescription;
        def.Category = r.Category;
        def.RequiresDateRange = r.RequiresDateRange;
        def.RequiresDepartmentFilter = r.RequiresDepartmentFilter;
        def.SupportedFormatsCsv = ReportDtoMapping.JoinFormats(r.SupportedFormats);
        def.ColumnsCsv = string.Join("|", r.Columns ?? Array.Empty<string>());

        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ── Delete ──────────────────────────────────────────────────────────────────
public sealed record DeleteReportDefinitionCommand(Guid Id) : IRequest<Result>;

internal sealed class DeleteReportDefinitionHandler(IApplicationDbContext db)
    : IRequestHandler<DeleteReportDefinitionCommand, Result>
{
    public async Task<Result> Handle(DeleteReportDefinitionCommand cmd, CancellationToken ct)
    {
        var def = await db.ReportDefinitions.FirstOrDefaultAsync(x => x.Id == cmd.Id, ct);
        if (def is null) return Result.NotFound();

        db.ReportDefinitions.Remove(def);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
