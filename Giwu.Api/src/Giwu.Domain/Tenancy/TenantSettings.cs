namespace Giwu.Domain.Tenancy;

// Mirrors the Hybrid client enums by integer value so JSON serialization matches.

public enum WeekStart
{
    Sunday = 0,
    Monday = 1,
}

public enum DateFormat
{
    UsLong   = 0,   // April 26, 2026
    UsShort  = 1,   // 4/26/2026
    PhLong   = 2,   // 26 April 2026
    PhShort  = 3,   // 26/04/2026
    Iso      = 4,   // 2026-04-26
}

public enum PayFrequency
{
    Weekly       = 0,
    BiWeekly     = 1,
    SemiMonthly  = 2,
    Monthly      = 3,
}

public enum NotificationChannel
{
    Email = 0,
    InApp = 1,
    Both  = 2,
    Off   = 3,
}

public enum SessionTimeout
{
    Minutes15 = 15,
    Minutes30 = 30,
    Hours1    = 60,
    Hours4    = 240,
    Hours8    = 480,
    Hours24   = 1440,
}

public class LocalizationSettings
{
    public string Timezone { get; set; } = "Asia/Manila";
    public DateFormat DateFormat { get; set; } = DateFormat.UsLong;
    public string CurrencyCode { get; set; } = "PHP";
    public string CurrencySymbol { get; set; } = "₱";
    public WeekStart WeekStart { get; set; } = WeekStart.Sunday;
    public int FiscalYearStartMonth { get; set; } = 1;
}

public class PayrollDefaults
{
    public PayFrequency PayFrequency { get; set; } = PayFrequency.SemiMonthly;
    public int FirstCutoffDay { get; set; } = 15;
    public int SecondCutoffDay { get; set; } = 30;
    public decimal RegularOvertimeRate { get; set; } = 1.25m;
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

public class BrandingSettings
{
    public string CompanyName { get; set; } = "Giwu HR";
    public string LogoDataUrl { get; set; } = "";
    public bool IsDark { get; set; } = false;
    public string AccentColor { get; set; } = "#1D9E75";
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
    public string IpWhitelist { get; set; } = "";
}
