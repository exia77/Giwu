using Giwu.Domain.Reports;

namespace Giwu.Contracts.Reports;

// ── ReportDefinition ─────────────────────────────────────────────
public sealed record ReportDefinitionDto(
    string Id,
    string Code,
    string Name,
    string ShortDescription,
    string LongDescription,
    ReportCategory Category,
    bool IsCustom,
    bool RequiresDateRange,
    bool RequiresDepartmentFilter,
    IReadOnlyList<ReportFormat> SupportedFormats,
    IReadOnlyList<string> Columns,
    string? RegulatoryReference);

public sealed record CreateReportDefinitionRequest(
    string Name,
    string ShortDescription,
    string LongDescription,
    ReportCategory Category,
    bool RequiresDateRange,
    bool RequiresDepartmentFilter,
    IReadOnlyList<ReportFormat> SupportedFormats,
    IReadOnlyList<string> Columns);

public sealed record UpdateReportDefinitionRequest(
    string Name,
    string ShortDescription,
    string LongDescription,
    ReportCategory Category,
    bool RequiresDateRange,
    bool RequiresDepartmentFilter,
    IReadOnlyList<ReportFormat> SupportedFormats,
    IReadOnlyList<string> Columns);

// ── ReportSchedule ───────────────────────────────────────────────
public sealed record ReportScheduleDto(
    Guid Id,
    string DefinitionId,
    string DefinitionName,
    ReportCategory Category,
    ScheduleFrequency Frequency,
    string Cadence,
    IReadOnlyList<string> Recipients,
    ReportFormat Format,
    DateTime NextRunAt,
    DateTime? LastRunAt,
    bool IsActive);

public sealed record CreateReportScheduleRequest(
    string DefinitionId,
    string DefinitionName,
    ReportCategory Category,
    ScheduleFrequency Frequency,
    string Cadence,
    IReadOnlyList<string> Recipients,
    ReportFormat Format,
    DateTime NextRunAt);

public sealed record UpdateReportScheduleRequest(
    ScheduleFrequency Frequency,
    string Cadence,
    IReadOnlyList<string> Recipients,
    ReportFormat Format,
    DateTime NextRunAt,
    bool IsActive);

// ── ReportRun ────────────────────────────────────────────────────
public sealed record ReportRunDto(
    Guid Id,
    string DefinitionId,
    string DefinitionName,
    ReportCategory Category,
    ReportFormat Format,
    DateTime QueuedAt,
    DateTime? CompletedAt,
    ReportRunStatus Status,
    string RanByDisplay,
    DateTime? PeriodStart,
    DateTime? PeriodEnd,
    IReadOnlyList<string> Departments,
    int RowCount,
    long FileSizeBytes,
    string? FileName,
    string? ErrorMessage,
    Guid? ScheduleId);

public sealed record QueueReportRunRequest(
    string DefinitionId,
    string DefinitionName,
    ReportCategory Category,
    ReportFormat Format,
    DateTime? PeriodStart,
    DateTime? PeriodEnd,
    IReadOnlyList<string> Departments);

// ── ComplianceDeadline ───────────────────────────────────────────
public sealed record ComplianceDeadlineDto(
    Guid Id,
    string Agency,
    string FormCode,
    string Name,
    string Description,
    DateOnly DueDate,
    string PeriodCovered,
    bool IsFiled,
    DateOnly? FiledOn,
    string? RelatedReportCode);

public sealed record CreateComplianceDeadlineRequest(
    string Agency,
    string FormCode,
    string Name,
    string Description,
    DateOnly DueDate,
    string PeriodCovered,
    string? RelatedReportCode);

public sealed record UpdateComplianceDeadlineRequest(
    string Agency,
    string FormCode,
    string Name,
    string Description,
    DateOnly DueDate,
    string PeriodCovered,
    string? RelatedReportCode);
