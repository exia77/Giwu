namespace Giwu.HRMS.Hybrid.Models;

public class AttendanceEmployee
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Init { get; set; } = "";
    public string Av { get; set; } = "";
    public string Role { get; set; } = "";
    public string Dept { get; set; } = "";
    public string Status { get; set; } = "";
    public string Tin { get; set; } = "";
    public string Tout { get; set; } = "";
    public string Reason { get; set; } = "";
    public bool Pending { get; set; }
}

public record AttendanceTrendPoint(string D, int V, bool Today = false);

public record ClockInResult(string EmployeeId, string Status, string Time);

public static class AttendanceFormatting
{
    public static string BadgeClass(string s) => s switch
    {
        "Present" => "badge-present",
        "Absent"  => "badge-absent",
        "Late"    => "badge-late",
        "Leave"   => "badge-leave",
        "WFH"     => "badge-wfh",
        _         => ""
    };

    public static string AvatarStyle(string av) => av switch
    {
        "green"   => "background:var(--att-green-light);color:var(--att-green-text)",
        "blue"    => "background:var(--att-blue-light);color:var(--att-blue-text)",
        "amber"   => "background:var(--att-amber-light);color:var(--att-amber-text)",
        "red"     => "background:var(--att-red-light);color:var(--att-red-text)",
        "purple"  => "background:var(--att-purple-light);color:var(--att-purple-text)",
        "primary" => "background:var(--att-green-light);color:var(--att-green-text)",
        _         => ""
    };

    public static string FmtTime(string t)
    {
        if (string.IsNullOrEmpty(t)) return "—";
        var p = t.Split(':');
        if (p.Length < 2 || !int.TryParse(p[0], out var h)) return "—";
        var ampm = h >= 12 ? "PM" : "AM";
        var dispH = h % 12 == 0 ? 12 : h % 12;
        return $"{dispH}:{p[1]} {ampm}";
    }

    public static double? HoursWorked(string tin, string tout)
    {
        if (string.IsNullOrEmpty(tin)) return null;
        var end = string.IsNullOrEmpty(tout) ? DateTime.Now.ToString("HH:mm") : tout;
        var a = tin.Split(':').Select(int.Parse).ToArray();
        var b = end.Split(':').Select(int.Parse).ToArray();
        return ((b[0] - a[0]) * 60 + (b[1] - a[1])) / 60.0;
    }

    public static string FmtHours(double? h) => h.HasValue ? $"{h.Value:F1}h" : "—";
}
