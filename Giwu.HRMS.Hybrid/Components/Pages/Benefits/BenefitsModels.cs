namespace Giwu.HRMS.Hybrid.Models;

// â”€â”€â”€ Core entities â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

public enum BenefitCategory
{
    Health,         // HMO, dental, vision
    Insurance,      // group life, accident, critical illness
    Allowance,      // rice, transport, meal, communication
    Retirement,     // SSS, Pag-IBIG MP2, company retirement
    Loan,           // company salary loan, SSS loan, Pag-IBIG MPL
    Wellness,       // gym, mental health, EAP
    Education,      // tuition assistance, training budget
    Leave,          // VL, SL, EL, ML, PL credits (entitlement only)
    Other,
}

public class BenefitProgram
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Provider { get; set; } = "";        // "Maxicare", "Sun Life", "In-house"
    public BenefitCategory Category { get; set; }
    public string Description { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public bool IsMandatory { get; set; }              // auto-enroll all employees (e.g. group life)
    public string Eligibility { get; set; } = "";      // "Regular employees only", "After 6 months", etc.
    public DateTime EffectiveDate { get; set; }
    public DateTime? RenewalDate { get; set; }

    // Cost split
    public decimal MonthlyCostPerEmployee { get; set; } // total program cost
    public decimal EmployerShare { get; set; }          // â‚± amount or â€” see ShareIsPercent
    public decimal EmployeeShare { get; set; }
    public bool ShareIsPercent { get; set; } = false;   // if true, shares are %, else â‚±

    // Tiers â€” for HMO/insurance plans with multiple coverage levels
    public List<BenefitTier> Tiers { get; set; } = new();
}

public class BenefitTier
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";              // "Standard", "Gold", "Platinum"
    public decimal CoverageAmount { get; set; }         // e.g. â‚±150k MBL
    public decimal MonthlyCost { get; set; }
    public string Inclusions { get; set; } = "";        // free-form description
}

public class BenefitEnrollment
{
    public string Id { get; set; } = "";
    public string EmployeeId { get; set; } = "";
    public string ProgramId { get; set; } = "";
    public string? TierId { get; set; }                 // null if program has no tiers
    public DateTime EnrolledOn { get; set; }
    public DateTime? EndDate { get; set; }              // null = active
    public string Status { get; set; } = "Active";    // Active / Pending / Terminated / Suspended
    public decimal MonthlyContribution { get; set; }    // employee's share at time of enrollment
    public string Notes { get; set; } = "";
}

public class Dependent
{
    public string Id { get; set; } = "";
    public string EmployeeId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Relationship { get; set; } = "";    // Spouse / Child / Parent
    public DateTime BirthDate { get; set; }
    public string Gender { get; set; } = "";
    public bool IsEnrolledInHmo { get; set; }
}

public enum BenefitRequestType { Claim, Loan, Reimbursement }
public enum BenefitRequestStatus { Pending, Approved, Released, Rejected, Repaying, Closed }

public class BenefitRequest
{
    public string Id { get; set; } = "";
    public string EmployeeId { get; set; } = "";
    public string EmployeeName { get; set; } = "";
    public string EmployeeInit { get; set; } = "";
    public string EmployeeAv { get; set; } = "";
    public BenefitRequestType Type { get; set; }
    public string ProgramId { get; set; } = "";
    public string ProgramName { get; set; } = "";       // denormalized for table display
    public decimal Amount { get; set; }
    public DateTime RequestedOn { get; set; }
    public DateTime? ResolvedOn { get; set; }
    public BenefitRequestStatus Status { get; set; }
    public string Reason { get; set; } = "";

    // Loan-specific
    public int? TermMonths { get; set; }
    public decimal? MonthlyDeduction { get; set; }
    public decimal? OutstandingBalance { get; set; }
}

// â”€â”€â”€ BenefitsBundle: convenience for per-employee dialogs â”€â”€â”€â”€â”€â”€â”€

public class EmployeeBenefitsView
{
    public string EmployeeId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Init { get; set; } = "";
    public string Av { get; set; } = "";
    public string Role { get; set; } = "";
    public string Dept { get; set; } = "";
    public DateTime HireDate { get; set; }

    public List<BenefitEnrollment> Enrollments { get; set; } = new();
    public List<Dependent> Dependents { get; set; } = new();
    public List<BenefitRequest> Requests { get; set; } = new();
}

// â”€â”€â”€ Helpers â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

public static class BenefitsFormatting
{
    public static string CategoryLabel(BenefitCategory c) => c switch
    {
        BenefitCategory.Health      => "Health",
        BenefitCategory.Insurance   => "Insurance",
        BenefitCategory.Allowance   => "Allowance",
        BenefitCategory.Retirement  => "Retirement",
        BenefitCategory.Loan        => "Loan",
        BenefitCategory.Wellness    => "Wellness",
        BenefitCategory.Education   => "Education",
        BenefitCategory.Leave       => "Leave",
        BenefitCategory.Other       => "Other",
        _ => "â€”"
    };

    public static string CategoryBadgeClass(BenefitCategory c) => c switch
    {
        BenefitCategory.Health     => "ben-badge-health",
        BenefitCategory.Insurance  => "ben-badge-insurance",
        BenefitCategory.Allowance  => "ben-badge-allowance",
        BenefitCategory.Retirement => "ben-badge-retirement",
        BenefitCategory.Loan       => "ben-badge-loan",
        BenefitCategory.Wellness   => "ben-badge-wellness",
        BenefitCategory.Education  => "ben-badge-education",
        BenefitCategory.Leave      => "ben-badge-leave",
        _ => "ben-badge-other"
    };

    public static string CategoryIcon(BenefitCategory c) => c switch
    {
        BenefitCategory.Health     => MudBlazor.Icons.Material.Outlined.HealthAndSafety,
        BenefitCategory.Insurance  => MudBlazor.Icons.Material.Outlined.Shield,
        BenefitCategory.Allowance  => MudBlazor.Icons.Material.Outlined.Restaurant,
        BenefitCategory.Retirement => MudBlazor.Icons.Material.Outlined.Savings,
        BenefitCategory.Loan       => MudBlazor.Icons.Material.Outlined.AccountBalance,
        BenefitCategory.Wellness   => MudBlazor.Icons.Material.Outlined.SelfImprovement,
        BenefitCategory.Education  => MudBlazor.Icons.Material.Outlined.School,
        BenefitCategory.Leave      => MudBlazor.Icons.Material.Outlined.BeachAccess,
        _ => MudBlazor.Icons.Material.Outlined.CardGiftcard
    };

    public static string RequestStatusBadgeClass(BenefitRequestStatus s) => s switch
    {
        BenefitRequestStatus.Pending  => "ben-badge-pending",
        BenefitRequestStatus.Approved => "ben-badge-approved",
        BenefitRequestStatus.Released => "ben-badge-released",
        BenefitRequestStatus.Rejected => "ben-badge-rejected",
        BenefitRequestStatus.Repaying => "ben-badge-repaying",
        BenefitRequestStatus.Closed   => "ben-badge-closed",
        _ => ""
    };

    public static string RequestStatusLabel(BenefitRequestStatus s) => s.ToString();

    public static string RequestTypeLabel(BenefitRequestType t) => t switch
    {
        BenefitRequestType.Claim         => "Claim",
        BenefitRequestType.Loan          => "Loan",
        BenefitRequestType.Reimbursement => "Reimbursement",
        _ => "â€”"
    };

    public static string AvatarStyle(string av) => av switch
    {
        "green"   => "background:var(--ben-green-light);color:var(--ben-green-text)",
        "blue"    => "background:var(--ben-blue-light);color:var(--ben-blue-text)",
        "amber"   => "background:var(--ben-amber-light);color:var(--ben-amber-text)",
        "red"     => "background:var(--ben-red-light);color:var(--ben-red-text)",
        "purple"  => "background:var(--ben-purple-light);color:var(--ben-purple-text)",
        "primary" => "background:var(--ben-green-light);color:var(--ben-green-text)",
        _ => ""
    };

    public static string Money(decimal v) =>
        "â‚±" + v.ToString("N2", System.Globalization.CultureInfo.InvariantCulture);

    public static string MoneyCompact(decimal v)
    {
        if (Math.Abs(v) >= 1_000_000m) return $"â‚±{v/1_000_000m:F2}M";
        if (Math.Abs(v) >= 1_000m)     return $"â‚±{v/1_000m:F0}k";
        return $"â‚±{v:F0}";
    }

    public static string FmtDate(DateTime? d) => d.HasValue ? d.Value.ToString("MMM d, yyyy") : "â€”";
    public static string FmtDateShort(DateTime d) => d.ToString("MMM d");

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

    public static int AgeFromBirthdate(DateTime birth)
    {
        var today = DateTime.Today;
        var age = today.Year - birth.Year;
        if (birth.Date > today.AddYears(-age)) age--;
        return age;
    }
}
