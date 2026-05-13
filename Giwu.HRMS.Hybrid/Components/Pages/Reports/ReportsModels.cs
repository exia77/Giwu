namespace Giwu.HRMS.Hybrid.Models;

// ─── Categories & enums ─────────────────────────────────────────

public enum ReportCategory
{
    Payroll,
    Attendance,
    People,         // headcount, demographics, tenure
    Recruitment,
    Benefits,
    Leave,
    Compliance,     // BIR, SSS, PhilHealth, Pag-IBIG, DOLE
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
    BiWeekly,       // 15th and 30th — common for PH payroll
    Monthly,
    Quarterly,
    Yearly,
}

// ─── Core entities ──────────────────────────────────────────────

public class ReportDefinition
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string ShortDescription { get; set; } = "";
    public string LongDescription { get; set; } = "";
    public ReportCategory Category { get; set; }
    public bool IsCompliance { get; set; }              // BIR/SSS/PhilHealth/Pag-IBIG/DOLE
    public string? RegulatoryReference { get; set; }    // "BIR Form 2316", "SSS R-3", etc.
    public List<ReportFormat> SupportedFormats { get; set; } = new() { ReportFormat.Csv, ReportFormat.Excel };
    public bool RequiresDateRange { get; set; } = true;
    public bool RequiresDepartmentFilter { get; set; } = false;
    public List<string> Columns { get; set; } = new();  // preview columns
    public bool IsCustom { get; set; }                  // user-defined template
    public string? CreatedBy { get; set; }
    public DateTime? CreatedOn { get; set; }
}

public class ReportRun
{
    public string Id { get; set; } = "";
    public string DefinitionId { get; set; } = "";
    public string DefinitionName { get; set; } = "";
    public ReportCategory Category { get; set; }
    public ReportFormat Format { get; set; }
    public DateTime QueuedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public ReportRunStatus Status { get; set; }
    public string RanBy { get; set; } = "";
    public string RanByInit { get; set; } = "";
    public string RanByAv { get; set; } = "";
    public DateTime? PeriodStart { get; set; }
    public DateTime? PeriodEnd { get; set; }
    public List<string> Departments { get; set; } = new();
    public int RowCount { get; set; }
    public long FileSizeBytes { get; set; }
    public string? FileName { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ScheduleId { get; set; }             // null if run on-demand
}

public class ReportSchedule
{
    public string Id { get; set; } = "";
    public string DefinitionId { get; set; } = "";
    public string DefinitionName { get; set; } = "";
    public ReportCategory Category { get; set; }
    public ScheduleFrequency Frequency { get; set; }
    public string Cadence { get; set; } = "";           // "Every Monday at 8 AM", "Every 15th and 30th"
    public List<string> Recipients { get; set; } = new(); // email addresses
    public ReportFormat Format { get; set; }
    public DateTime? LastRunAt { get; set; }
    public DateTime NextRunAt { get; set; }
    public bool IsActive { get; set; } = true;
    public string CreatedBy { get; set; } = "";
    public DateTime CreatedOn { get; set; }
}

public class ComplianceDeadline
{
    public string Id { get; set; } = "";
    public string Agency { get; set; } = "";            // BIR, SSS, PhilHealth, Pag-IBIG, DOLE
    public string FormCode { get; set; } = "";          // "1601-C", "R-3", "RF-1", "MCRF"
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime DueDate { get; set; }
    public string PeriodCovered { get; set; } = "";     // "Q1 2026", "March 2026"
    public bool IsFiled { get; set; }
    public DateTime? FiledOn { get; set; }
    public string? RelatedReportId { get; set; }        // Report definition ID that produces this filing
}

// ─── Helpers ────────────────────────────────────────────────────

public static class ReportsFormatting
{
    public static string CategoryLabel(ReportCategory c) => c switch
    {
        ReportCategory.Payroll     => "Payroll",
        ReportCategory.Attendance  => "Attendance",
        ReportCategory.People      => "People",
        ReportCategory.Recruitment => "Recruitment",
        ReportCategory.Benefits    => "Benefits",
        ReportCategory.Leave       => "Leave",
        ReportCategory.Compliance  => "Compliance",
        ReportCategory.Custom      => "Custom",
        _ => "Other"
    };

    public static string CategoryBadgeClass(ReportCategory c) => c switch
    {
        ReportCategory.Payroll     => "rep-badge-payroll",
        ReportCategory.Attendance  => "rep-badge-attendance",
        ReportCategory.People      => "rep-badge-people",
        ReportCategory.Recruitment => "rep-badge-recruitment",
        ReportCategory.Benefits    => "rep-badge-benefits",
        ReportCategory.Leave       => "rep-badge-leave",
        ReportCategory.Compliance  => "rep-badge-compliance",
        ReportCategory.Custom      => "rep-badge-custom",
        _ => "rep-badge-other"
    };

    public static string CategoryIcon(ReportCategory c) => c switch
    {
        ReportCategory.Payroll     => MudBlazor.Icons.Material.Outlined.Payments,
        ReportCategory.Attendance  => MudBlazor.Icons.Material.Outlined.AccessTime,
        ReportCategory.People      => MudBlazor.Icons.Material.Outlined.Groups,
        ReportCategory.Recruitment => MudBlazor.Icons.Material.Outlined.WorkOutline,
        ReportCategory.Benefits    => MudBlazor.Icons.Material.Outlined.VolunteerActivism,
        ReportCategory.Leave       => MudBlazor.Icons.Material.Outlined.BeachAccess,
        ReportCategory.Compliance  => MudBlazor.Icons.Material.Outlined.Gavel,
        ReportCategory.Custom      => MudBlazor.Icons.Material.Outlined.Tune,
        _ => MudBlazor.Icons.Material.Outlined.Description
    };

    public static string FormatLabel(ReportFormat f) => f switch
    {
        ReportFormat.Csv   => "CSV",
        ReportFormat.Excel => "Excel",
        ReportFormat.Pdf   => "PDF",
        _ => "—"
    };

    public static string FormatIcon(ReportFormat f) => f switch
    {
        ReportFormat.Csv   => MudBlazor.Icons.Material.Outlined.Description,
        ReportFormat.Excel => MudBlazor.Icons.Material.Outlined.GridOn,
        ReportFormat.Pdf   => MudBlazor.Icons.Material.Outlined.PictureAsPdf,
        _ => MudBlazor.Icons.Material.Outlined.InsertDriveFile
    };

    public static string FormatExtension(ReportFormat f) => f switch
    {
        ReportFormat.Csv   => "csv",
        ReportFormat.Excel => "xlsx",
        ReportFormat.Pdf   => "pdf",
        _ => "txt"
    };

    public static string FrequencyLabel(ScheduleFrequency f) => f switch
    {
        ScheduleFrequency.Daily     => "Daily",
        ScheduleFrequency.Weekly    => "Weekly",
        ScheduleFrequency.BiWeekly  => "Bi-monthly (15th & 30th)",
        ScheduleFrequency.Monthly   => "Monthly",
        ScheduleFrequency.Quarterly => "Quarterly",
        ScheduleFrequency.Yearly    => "Yearly",
        _ => "—"
    };

    public static string StatusBadgeClass(ReportRunStatus s) => s switch
    {
        ReportRunStatus.Queued    => "rep-status-queued",
        ReportRunStatus.Running   => "rep-status-running",
        ReportRunStatus.Completed => "rep-status-completed",
        ReportRunStatus.Failed    => "rep-status-failed",
        _ => ""
    };

    public static string StatusLabel(ReportRunStatus s) => s.ToString();

    public static string AgencyBadgeClass(string agency) => agency switch
    {
        "BIR"        => "rep-agency-bir",
        "SSS"        => "rep-agency-sss",
        "PhilHealth" => "rep-agency-philhealth",
        "Pag-IBIG"   => "rep-agency-pagibig",
        "DOLE"       => "rep-agency-dole",
        _ => "rep-agency-other"
    };

    public static string AvatarStyle(string av) => av switch
    {
        "green"   => "background:var(--rep-green-light);color:var(--rep-green-text)",
        "blue"    => "background:var(--rep-blue-light);color:var(--rep-blue-text)",
        "amber"   => "background:var(--rep-amber-light);color:var(--rep-amber-text)",
        "red"     => "background:var(--rep-red-light);color:var(--rep-red-text)",
        "purple"  => "background:var(--rep-purple-light);color:var(--rep-purple-text)",
        "primary" => "background:var(--rep-green-light);color:var(--rep-green-text)",
        _ => ""
    };

    public static string FmtDate(DateTime? d) => d.HasValue ? d.Value.ToString("MMM d, yyyy") : "—";
    public static string FmtDateShort(DateTime d) => d.ToString("MMM d");

    public static string RelativeTime(DateTime d)
    {
        var span = DateTime.Now - d;
        if (span.TotalSeconds < 0)
        {
            // Future
            var f = -span;
            if (f.TotalMinutes < 1) return "in a moment";
            if (f.TotalHours < 1)   return $"in {(int)f.TotalMinutes}m";
            if (f.TotalDays < 1)    return $"in {(int)f.TotalHours}h";
            if (f.TotalDays < 30)   return $"in {(int)f.TotalDays}d";
            return d.ToString("MMM d, yyyy");
        }
        if (span.TotalMinutes < 1) return "just now";
        if (span.TotalHours < 1)   return $"{(int)span.TotalMinutes}m ago";
        if (span.TotalDays < 1)    return $"{(int)span.TotalHours}h ago";
        if (span.TotalDays < 30)   return $"{(int)span.TotalDays}d ago";
        if (span.TotalDays < 365)  return $"{(int)(span.TotalDays/30)}mo ago";
        return d.ToString("MMM d, yyyy");
    }

    public static string FmtFileSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        if (bytes < 1024L * 1024 * 1024) return $"{bytes / (1024.0 * 1024):F1} MB";
        return $"{bytes / (1024.0 * 1024 * 1024):F1} GB";
    }

    public static string DaysUntilLabel(DateTime d)
    {
        var days = (d.Date - DateTime.Today).Days;
        if (days < 0) return $"{-days}d overdue";
        if (days == 0) return "due today";
        if (days == 1) return "due tomorrow";
        if (days <= 30) return $"in {days}d";
        if (days <= 60) return $"in {days/7}w";
        return d.ToString("MMM d");
    }

    public static string DaysUntilSeverity(DateTime d, bool isFiled)
    {
        if (isFiled) return "ok";
        var days = (d.Date - DateTime.Today).Days;
        if (days < 0) return "overdue";
        if (days <= 7) return "urgent";
        if (days <= 30) return "soon";
        return "future";
    }
}
