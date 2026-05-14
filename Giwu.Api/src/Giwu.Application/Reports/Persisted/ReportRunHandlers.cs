using FluentValidation;
using Giwu.Application.Common;
using Giwu.Contracts.Reports;
using Giwu.Domain.Reports;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Giwu.Application.Reports.Persisted;

// ── List ────────────────────────────────────────────────────────────────────
public sealed record ListReportRunsQuery(int Limit = 200)
    : IRequest<Result<IReadOnlyList<ReportRunDto>>>;

internal sealed class ListReportRunsHandler(IApplicationDbContext db)
    : IRequestHandler<ListReportRunsQuery, Result<IReadOnlyList<ReportRunDto>>>
{
    public async Task<Result<IReadOnlyList<ReportRunDto>>> Handle(
        ListReportRunsQuery q, CancellationToken ct)
    {
        var rows = await db.ReportRuns
            .OrderByDescending(r => r.QueuedAt)
            .Take(Math.Clamp(q.Limit, 1, 1000))
            .ToListAsync(ct);
        return Result<IReadOnlyList<ReportRunDto>>.Success(rows.Select(ReportDtoMapping.ToDto).ToList());
    }
}

// ── Queue (and complete-now for demo) ───────────────────────────────────────
public sealed record QueueReportRunCommand(QueueReportRunRequest Request)
    : IRequest<Result<ReportRunDto>>;

public sealed class QueueReportRunValidator : AbstractValidator<QueueReportRunCommand>
{
    public QueueReportRunValidator()
    {
        RuleFor(x => x.Request.DefinitionId).NotEmpty();
        RuleFor(x => x.Request.DefinitionName).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Request.Format).IsInEnum();
        RuleFor(x => x.Request.Category).IsInEnum();
    }
}

internal sealed class QueueReportRunHandler(
    IApplicationDbContext db,
    ICurrentUser user,
    TimeProvider clock)
    : IRequestHandler<QueueReportRunCommand, Result<ReportRunDto>>
{
    public async Task<Result<ReportRunDto>> Handle(QueueReportRunCommand cmd, CancellationToken ct)
    {
        var r = cmd.Request;
        var now = clock.GetUtcNow().UtcDateTime;
        var rowCount = SimulateRowCount(r.Category);
        var run = new ReportRun
        {
            DefinitionId = r.DefinitionId,
            DefinitionName = r.DefinitionName,
            Category = r.Category,
            Format = r.Format,
            QueuedAt = now,
            CompletedAt = now.AddSeconds(2),
            Status = ReportRunStatus.Completed,
            RanByUserId = user.IsAuthenticated ? user.Id : null,
            RanByDisplay = user.IsAuthenticated ? user.DisplayName : "system",
            PeriodStart = AsUtc(r.PeriodStart),
            PeriodEnd = AsUtc(r.PeriodEnd),
            DepartmentsCsv = string.Join(",", r.Departments ?? Array.Empty<string>()),
            RowCount = rowCount,
            FileSizeBytes = SimulateFileSize(rowCount, r.Format),
            FileName = $"{r.DefinitionId}_{now:yyyyMMdd_HHmmss}.{Ext(r.Format)}",
        };
        db.ReportRuns.Add(run);
        await db.SaveChangesAsync(ct);
        return Result<ReportRunDto>.Success(ReportDtoMapping.ToDto(run));
    }

    private static int SimulateRowCount(ReportCategory c) => c switch
    {
        ReportCategory.Payroll     => 38,
        ReportCategory.Attendance  => 800,
        ReportCategory.People      => 38,
        ReportCategory.Recruitment => 22,
        ReportCategory.Benefits    => 65,
        ReportCategory.Leave       => 120,
        ReportCategory.Compliance  => 38,
        _ => 50,
    };

    private static long SimulateFileSize(int rows, ReportFormat fmt)
    {
        long basePerRow = fmt switch
        {
            ReportFormat.Csv   => 120,
            ReportFormat.Excel => 220,
            ReportFormat.Pdf   => 960,
            _ => 120,
        };
        return Math.Max(1024, rows * basePerRow);
    }

    private static string Ext(ReportFormat f) => f switch
    {
        ReportFormat.Csv   => "csv",
        ReportFormat.Excel => "xlsx",
        ReportFormat.Pdf   => "pdf",
        _ => "txt",
    };

    // JSON deserialization yields DateTime with Kind=Unspecified, which Npgsql
    // rejects for `timestamp with time zone` columns. Treat the wire value as
    // already-UTC (the API contract); convert Local explicitly if it ever shows up.
    private static DateTime? AsUtc(DateTime? d) => d?.Kind switch
    {
        null                    => null,
        DateTimeKind.Utc        => d,
        DateTimeKind.Local      => d.Value.ToUniversalTime(),
        _                       => DateTime.SpecifyKind(d.Value, DateTimeKind.Utc),
    };
}
