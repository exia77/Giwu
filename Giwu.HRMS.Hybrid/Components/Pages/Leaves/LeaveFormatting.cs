using Giwu.HRMS.Hybrid.Models.Leave;

namespace Giwu.HRMS.Hybrid.Components.Pages.Leaves;

/// <summary>
/// UI-side formatters for leave data — labels, badge classes, date and day
/// formatters, and a working-days helper. These are display concerns, not
/// model logic, so they live next to the page that consumes them rather than
/// in <c>Models/</c>.
/// </summary>
public static class LeaveFormatting
{
    public static string CategoryLabel(LeaveCategory c) => c switch
    {
        LeaveCategory.Vacation    => "Vacation",
        LeaveCategory.Sick        => "Sick",
        LeaveCategory.Emergency   => "Emergency",
        LeaveCategory.Maternity   => "Maternity",
        LeaveCategory.Paternity   => "Paternity",
        LeaveCategory.SoloParent  => "Solo Parent",
        LeaveCategory.Bereavement => "Bereavement",
        LeaveCategory.Magna       => "Magna Carta",
        LeaveCategory.Vawc        => "VAWC",
        LeaveCategory.Sil         => "SIL",
        LeaveCategory.Birthday    => "Birthday",
        LeaveCategory.Unpaid      => "Unpaid",
        _ => "Other"
    };

    public static string CategoryBadgeClass(LeaveCategory c) => c switch
    {
        LeaveCategory.Vacation    => "leave-badge-vacation",
        LeaveCategory.Sick        => "leave-badge-sick",
        LeaveCategory.Emergency   => "leave-badge-emergency",
        LeaveCategory.Maternity   => "leave-badge-maternity",
        LeaveCategory.Paternity   => "leave-badge-paternity",
        LeaveCategory.SoloParent  => "leave-badge-solo",
        LeaveCategory.Bereavement => "leave-badge-bereavement",
        LeaveCategory.Magna       => "leave-badge-magna",
        LeaveCategory.Vawc        => "leave-badge-vawc",
        LeaveCategory.Sil         => "leave-badge-sil",
        LeaveCategory.Birthday    => "leave-badge-birthday",
        LeaveCategory.Unpaid      => "leave-badge-unpaid",
        _ => "leave-badge-other"
    };

    public static string CategoryDotColor(LeaveCategory c) => c switch
    {
        LeaveCategory.Vacation    => "var(--leave-blue)",
        LeaveCategory.Sick        => "var(--leave-red)",
        LeaveCategory.Emergency   => "var(--leave-amber)",
        LeaveCategory.Maternity   => "var(--leave-pink)",
        LeaveCategory.Paternity   => "var(--leave-teal)",
        LeaveCategory.SoloParent  => "var(--leave-purple)",
        LeaveCategory.Bereavement => "var(--leave-grey)",
        LeaveCategory.Magna       => "var(--leave-pink)",
        LeaveCategory.Vawc        => "var(--leave-red)",
        LeaveCategory.Sil         => "var(--leave-green)",
        LeaveCategory.Birthday    => "var(--leave-amber)",
        LeaveCategory.Unpaid      => "var(--leave-grey)",
        _ => "var(--mud-palette-divider)"
    };

    public static string StatusBadgeClass(LeaveRequestStatus s) => s switch
    {
        LeaveRequestStatus.Pending   => "leave-status-pending",
        LeaveRequestStatus.Approved  => "leave-status-approved",
        LeaveRequestStatus.Rejected  => "leave-status-rejected",
        LeaveRequestStatus.Cancelled => "leave-status-cancelled",
        LeaveRequestStatus.Taken     => "leave-status-taken",
        _ => ""
    };

    public static string StatusLabel(LeaveRequestStatus s) => s switch
    {
        LeaveRequestStatus.Pending   => "Pending",
        LeaveRequestStatus.Approved  => "Approved",
        LeaveRequestStatus.Rejected  => "Rejected",
        LeaveRequestStatus.Cancelled => "Cancelled",
        LeaveRequestStatus.Taken     => "Taken",
        _ => "—"
    };

    public static string AccrualModeLabel(LeaveAccrualMode m) => m switch
    {
        LeaveAccrualMode.Yearly   => "Granted yearly",
        LeaveAccrualMode.Monthly  => "Accrues monthly",
        LeaveAccrualMode.PerEvent => "Per qualifying event",
        LeaveAccrualMode.Tenure   => "After tenure milestone",
        _ => "—"
    };

    public static string AvatarStyle(string av) => av switch
    {
        "green"   => "background:var(--leave-green-light);color:var(--leave-green-text)",
        "blue"    => "background:var(--leave-blue-light);color:var(--leave-blue-text)",
        "amber"   => "background:var(--leave-amber-light);color:var(--leave-amber-text)",
        "red"     => "background:var(--leave-red-light);color:var(--leave-red-text)",
        "purple"  => "background:var(--leave-purple-light);color:var(--leave-purple-text)",
        "primary" => "background:var(--leave-green-light);color:var(--leave-green-text)",
        _ => ""
    };

    public static string FmtDate(DateTime? d) => d.HasValue ? d.Value.ToString("MMM d, yyyy") : "—";
    public static string FmtDateShort(DateTime d) => d.ToString("MMM d");
    public static string FmtDateRange(DateTime s, DateTime e)
    {
        if (s.Date == e.Date) return s.ToString("MMM d, yyyy");
        if (s.Year == e.Year && s.Month == e.Month)
            return $"{s:MMM d}–{e.Day}, {e:yyyy}";
        if (s.Year == e.Year)
            return $"{s:MMM d} – {e:MMM d}, {e:yyyy}";
        return $"{s:MMM d, yyyy} – {e:MMM d, yyyy}";
    }

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

    /// <summary>Counts working days (Mon–Fri) between two dates inclusive. PH holidays are not subtracted in this demo.</summary>
    public static decimal WorkingDays(DateTime start, DateTime end, bool isHalfDay)
    {
        if (end < start) return 0m;
        var days = 0;
        for (var d = start.Date; d <= end.Date; d = d.AddDays(1))
        {
            if (d.DayOfWeek != DayOfWeek.Saturday && d.DayOfWeek != DayOfWeek.Sunday)
                days++;
        }
        if (isHalfDay && days >= 1) return days - 0.5m;
        return days;
    }

    public static string FmtDays(decimal d)
    {
        if (d == Math.Floor(d)) return $"{d:F0} day{(d == 1m ? "" : "s")}";
        return $"{d:F1} days";
    }
}
