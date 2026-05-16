namespace Giwu.HRMS.Hybrid.Models;

// ─── Core entities ──────────────────────────────────────────────

public class JobRequisition
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Dept { get; set; } = "";
    public string Location { get; set; } = "";        // "Hybrid - BGC" / "Remote" / "On-site - Makati"
    public string EmploymentType { get; set; } = "";  // "Full-time" / "Contract" / "Part-time"
    public int Openings { get; set; }
    public int Filled { get; set; }
    public DateTime PostedOn { get; set; }
    public DateTime? TargetFillBy { get; set; }
    public string Status { get; set; } = "";          // "Open" / "On Hold" / "Closed" / "Draft"
    public string Owner { get; set; } = "";           // recruiter initials
    public string OwnerAv { get; set; } = "";
    public decimal SalaryMin { get; set; }
    public decimal SalaryMax { get; set; }
    public string Description { get; set; } = "";
}

public class Candidate
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Init { get; set; } = "";
    public string Av { get; set; } = "";               // avatar color token
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string JobId { get; set; } = "";            // FK to JobRequisition
    public string Stage { get; set; } = "";            // Applied / Screening / Interview / Offer / Hired / Rejected
    public string Source { get; set; } = "";           // LinkedIn / Referral / Job Board / Website
    public DateTime AppliedOn { get; set; }
    public DateTime LastActivity { get; set; }
    public int Rating { get; set; }                    // 0-5
    public string Notes { get; set; } = "";
    public string ResumeUrl { get; set; } = "";        // placeholder
    public List<CandidateEvent> Timeline { get; set; } = new();
}

public record CandidateEvent(DateTime When, string Stage, string Note, string Actor);

public record StageChangeResult(string NewStage, string Note);

public class Interview
{
    public string Id { get; set; } = "";
    public string CandidateId { get; set; } = "";
    public DateTime ScheduledAt { get; set; }
    public int DurationMinutes { get; set; } = 45;
    public string Kind { get; set; } = "";             // Phone Screen / Technical / Panel / Final
    public string Interviewer { get; set; } = "";
    public string InterviewerInit { get; set; } = "";
    public string InterviewerAv { get; set; } = "";
    public string Location { get; set; } = "";         // "Zoom" / "Office - Conf A"
    public string Status { get; set; } = "Scheduled"; // Scheduled / Completed / Canceled
}

// ─── Helpers ────────────────────────────────────────────────────

public static class RecruitmentFormatting
{
    public static readonly string[] PipelineStages =
        new[] { "Applied", "Screening", "Interview", "Offer", "Hired" };

    public static string StageBadgeClass(string s) => s switch
    {
        "Applied"   => "rec-badge-applied",
        "Screening" => "rec-badge-screening",
        "Interview" => "rec-badge-interview",
        "Offer"     => "rec-badge-offer",
        "Hired"     => "rec-badge-hired",
        "Rejected"  => "rec-badge-rejected",
        _           => ""
    };

    public static string JobStatusBadgeClass(string s) => s switch
    {
        "Open"    => "rec-badge-hired",     // green
        "On Hold" => "rec-badge-interview", // amber
        "Closed"  => "rec-badge-rejected",  // red
        "Draft"   => "rec-badge-applied",   // blue
        _         => ""
    };

    public static string AvatarStyle(string av) => av switch
    {
        "green"   => "background:var(--giwu-status-success-bg);color:var(--giwu-status-success-fg)",
        "blue"    => "background:var(--giwu-status-info-bg);color:var(--giwu-status-info-fg)",
        "amber"   => "background:var(--giwu-status-warning-bg);color:var(--giwu-status-warning-fg)",
        "red"     => "background:var(--giwu-status-danger-bg);color:var(--giwu-status-danger-fg)",
        "purple"  => "background:var(--giwu-status-accent-bg);color:var(--giwu-status-accent-fg)",
        "primary" => "background:var(--giwu-status-success-bg);color:var(--giwu-status-success-fg)",
        _         => ""
    };

    public static string FmtDate(DateTime? d) => d.HasValue ? d.Value.ToString("MMM d, yyyy") : "—";
    public static string FmtDateShort(DateTime d) => d.ToString("MMM d");
    public static string FmtDateTime(DateTime d) => d.ToString("MMM d, h:mm tt");

    public static string RelativeTime(DateTime d)
    {
        var span = DateTime.Now - d;
        if (span.TotalMinutes < 1) return "just now";
        if (span.TotalHours < 1)   return $"{(int)span.TotalMinutes}m ago";
        if (span.TotalDays < 1)    return $"{(int)span.TotalHours}h ago";
        if (span.TotalDays < 30)   return $"{(int)span.TotalDays}d ago";
        if (span.TotalDays < 365)  return $"{(int)(span.TotalDays/30)}mo ago";
        return d.ToString("MMM d, yyyy");
    }

    public static string FmtSalary(decimal min, decimal max)
    {
        string F(decimal v) => v >= 1000 ? $"₱{v/1000:F0}k" : $"₱{v:F0}";
        return min == max ? F(min) : $"{F(min)} – {F(max)}";
    }
}
