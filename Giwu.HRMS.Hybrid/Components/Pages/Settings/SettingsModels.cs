namespace Giwu.HRMS.Hybrid.Models;

// ─── Enums ──────────────────────────────────────────────────────

public enum WeekStart { Sunday, Monday }

public enum DateFormat
{
    UsLong,         // April 26, 2026
    UsShort,        // 4/26/2026
    PhLong,         // 26 April 2026
    PhShort,        // 26/04/2026
    Iso,            // 2026-04-26
}

public enum PayFrequency
{
    Weekly,
    BiWeekly,           // every other week
    SemiMonthly,        // 15th and 30th — most common in PH
    Monthly,
}

public enum NotificationChannel
{
    Email,
    InApp,
    Both,
    Off,
}

public enum SessionTimeout
{
    Minutes15  = 15,
    Minutes30  = 30,
    Hours1     = 60,
    Hours4     = 240,
    Hours8     = 480,
    Hours24    = 1440,
}

// ─── Composite settings record ──────────────────────────────────

public class OrganizationProfile
{
    public string LegalName { get; set; } = "";
    public string TradeName { get; set; } = "";
    public string Address { get; set; } = "";
    public string City { get; set; } = "";
    public string Province { get; set; } = "";
    public string PostalCode { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Email { get; set; } = "";
    public string Website { get; set; } = "";

    // PH employer registration numbers
    public string Tin { get; set; } = "";              // BIR Tax Identification Number
    public string RdoCode { get; set; } = "";          // BIR Revenue District Office code
    public string SssEmployerNo { get; set; } = "";
    public string PhilHealthEmployerNo { get; set; } = "";
    public string PagibigEmployerNo { get; set; } = "";
    public string DoleEstablishmentNo { get; set; } = "";
}

public class LocalizationSettings
{
    public string Timezone { get; set; } = "Asia/Manila";
    public DateFormat DateFormat { get; set; } = DateFormat.UsLong;
    public string CurrencyCode { get; set; } = "PHP";
    public string CurrencySymbol { get; set; } = "₱";
    public WeekStart WeekStart { get; set; } = WeekStart.Sunday;
    public int FiscalYearStartMonth { get; set; } = 1;          // January
}

public class PayrollDefaults
{
    public PayFrequency PayFrequency { get; set; } = PayFrequency.SemiMonthly;
    public int FirstCutoffDay { get; set; } = 15;
    public int SecondCutoffDay { get; set; } = 30;
    public decimal RegularOvertimeRate { get; set; } = 1.25m;   // 125% of hourly
    public decimal RestDayOvertimeRate { get; set; } = 1.30m;
    public decimal HolidayOvertimeRate { get; set; } = 2.00m;
    public decimal NightDifferentialRate { get; set; } = 1.10m;
    public bool IncludeAllowanceIn13thMonth { get; set; } = false;
    public bool IncludeOtIn13thMonth { get; set; } = false;
    public bool RoundStatutoryDeductions { get; set; } = true;
}

public class NotificationSettings
{
    public NotificationChannel NewLeaveRequest    { get; set; } = NotificationChannel.Both;
    public NotificationChannel LeaveApproved      { get; set; } = NotificationChannel.Email;
    public NotificationChannel LeaveRejected      { get; set; } = NotificationChannel.Email;
    public NotificationChannel PayrollGenerated   { get; set; } = NotificationChannel.Both;
    public NotificationChannel PayslipReleased    { get; set; } = NotificationChannel.Email;
    public NotificationChannel ContractExpiring   { get; set; } = NotificationChannel.InApp;
    public NotificationChannel BirthdayReminder   { get; set; } = NotificationChannel.InApp;
    public NotificationChannel ComplianceDeadline { get; set; } = NotificationChannel.Both;
    public NotificationChannel BenefitsRenewal    { get; set; } = NotificationChannel.InApp;
    public NotificationChannel NewHireOnboarding  { get; set; } = NotificationChannel.Both;
}

public class SecuritySettings
{
    public bool RequireMfa { get; set; } = false;
    public int MinPasswordLength { get; set; } = 10;
    public bool RequireUppercase { get; set; } = true;
    public bool RequireLowercase { get; set; } = true;
    public bool RequireNumber { get; set; } = true;
    public bool RequireSpecial { get; set; } = false;
    public int PasswordExpiryDays { get; set; } = 90;
    public SessionTimeout SessionTimeout { get; set; } = SessionTimeout.Hours8;
    public int MaxFailedLoginAttempts { get; set; } = 5;
    public bool IpWhitelistEnabled { get; set; } = false;
    public string IpWhitelist { get; set; } = "";   // comma-separated CIDR
}

public class IntegrationStatus
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Provider { get; set; } = "";
    public bool IsConnected { get; set; }
    public DateTime? ConnectedOn { get; set; }
    public string Icon { get; set; } = "";
    public string Category { get; set; } = "";  // Time, Accounting, Email, Identity, Storage
}

// ─── Helpers ────────────────────────────────────────────────────

public static class SettingsFormatting
{
    public static string DateFormatLabel(DateFormat f) => f switch
    {
        DateFormat.UsLong  => "April 26, 2026",
        DateFormat.UsShort => "4/26/2026",
        DateFormat.PhLong  => "26 April 2026",
        DateFormat.PhShort => "26/04/2026",
        DateFormat.Iso     => "2026-04-26 (ISO)",
        _ => "—"
    };

    public static string PayFrequencyLabel(PayFrequency f) => f switch
    {
        PayFrequency.Weekly       => "Weekly",
        PayFrequency.BiWeekly     => "Bi-weekly (every 2 weeks)",
        PayFrequency.SemiMonthly  => "Semi-monthly (15th and 30th)",
        PayFrequency.Monthly      => "Monthly",
        _ => "—"
    };

    public static string SessionTimeoutLabel(SessionTimeout t) => t switch
    {
        SessionTimeout.Minutes15 => "15 minutes",
        SessionTimeout.Minutes30 => "30 minutes",
        SessionTimeout.Hours1    => "1 hour",
        SessionTimeout.Hours4    => "4 hours",
        SessionTimeout.Hours8    => "8 hours (work day)",
        SessionTimeout.Hours24   => "24 hours",
        _ => "—"
    };

    public static string NotificationChannelLabel(NotificationChannel c) => c switch
    {
        NotificationChannel.Email => "Email",
        NotificationChannel.InApp => "In-app only",
        NotificationChannel.Both  => "Email + In-app",
        NotificationChannel.Off   => "Off",
        _ => "—"
    };

    public static string MonthName(int month) => month switch
    {
        1  => "January", 2  => "February", 3  => "March",      4 => "April",
        5  => "May",     6  => "June",     7  => "July",       8 => "August",
        9  => "September", 10 => "October", 11 => "November", 12 => "December",
        _ => "—"
    };

    public static List<string> CommonTimezones() => new()
    {
        "Asia/Manila",
        "Asia/Singapore",
        "Asia/Hong_Kong",
        "Asia/Tokyo",
        "Asia/Seoul",
        "Australia/Sydney",
        "Europe/London",
        "America/New_York",
        "America/Los_Angeles",
        "UTC",
    };

    public static List<string> CommonAccentPresets() => new()
    {
        "#1D9E75",  // Default Giwu green
        "#185FA5",  // Blue
        "#534AB7",  // Purple
        "#B8336A",  // Pink
        "#0E7C7B",  // Teal
        "#BA7517",  // Amber
        "#E24B4A",  // Red
        "#1F2937",  // Slate (near-black)
    };
}
