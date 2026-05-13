using Giwu.Domain.Common;

namespace Giwu.Domain.Reports;

public enum ReportCategory
{
    Payroll,
    Attendance,
    People,
    Recruitment,
    Benefits,
    Leave,
    Compliance,
    Custom,
}

public enum ReportFormat
{
    Csv,
    Excel,
    Pdf,
}

public enum ReportRunStatus
{
    Queued,
    Running,
    Completed,
    Failed,
}

public enum ScheduleFrequency
{
    Daily,
    Weekly,
    BiWeekly,
    Monthly,
    Quarterly,
    Yearly,
}

/// <summary>
/// User-created custom report templates. Built-in definitions stay in code
/// and are merged with these at the API surface.
/// </summary>
public class ReportDefinition : AuditableEntity
{
    public string Code { get; set; } = "";            // "RD-CUSTOM-001", stable handle
    public string Name { get; set; } = "";
    public string ShortDescription { get; set; } = "";
    public string LongDescription { get; set; } = "";
    public ReportCategory Category { get; set; }
    public bool RequiresDateRange { get; set; } = true;
    public bool RequiresDepartmentFilter { get; set; }
    public string SupportedFormatsCsv { get; set; } = "Csv,Excel"; // serialized list
    public string ColumnsCsv { get; set; } = "";                    // serialized list
}

public class ReportSchedule : AuditableEntity
{
    public string DefinitionId { get; set; } = "";    // built-in code or DB GUID string
    public string DefinitionName { get; set; } = "";
    public ReportCategory Category { get; set; }
    public ScheduleFrequency Frequency { get; set; }
    public string Cadence { get; set; } = "";
    public string RecipientsCsv { get; set; } = "";   // comma-separated emails
    public ReportFormat Format { get; set; }
    public DateTime NextRunAt { get; set; }
    public DateTime? LastRunAt { get; set; }
    public bool IsActive { get; set; } = true;
}

public class ReportRun : AuditableEntity
{
    public string DefinitionId { get; set; } = "";
    public string DefinitionName { get; set; } = "";
    public ReportCategory Category { get; set; }
    public ReportFormat Format { get; set; }
    public DateTime QueuedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public ReportRunStatus Status { get; set; }
    public Guid? RanByUserId { get; set; }
    public string RanByDisplay { get; set; } = "";
    public DateTime? PeriodStart { get; set; }
    public DateTime? PeriodEnd { get; set; }
    public string DepartmentsCsv { get; set; } = "";
    public int RowCount { get; set; }
    public long FileSizeBytes { get; set; }
    public string? FileName { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid? ScheduleId { get; set; }
}

public class ComplianceDeadline : AuditableEntity
{
    public string Agency { get; set; } = "";
    public string FormCode { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public DateOnly DueDate { get; set; }
    public string PeriodCovered { get; set; } = "";
    public bool IsFiled { get; set; }
    public DateOnly? FiledOn { get; set; }
    public string? RelatedReportCode { get; set; }
}
