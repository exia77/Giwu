using Giwu.HRMS.Hybrid.Models;

namespace Giwu.HRMS.Hybrid.Components.Pages.Settings;

/// <summary>
/// In-memory store for application settings that aren't already on ThemeService
/// (which handles CompanyName, LogoDataUrl, and IsDark). Register this as a
/// singleton in your Program.cs:
/// <code>
///     builder.Services.AddSingleton&lt;SettingsService&gt;();
/// </code>
///
/// In production, persist these to a database, file, or your config store of
/// choice — replace the field-backed properties with reads/writes against your
/// store and raise OnChange when anything mutates.
/// </summary>
public class SettingsService
{
    public OrganizationProfile Organization { get; } = SeedOrganization();
    public LocalizationSettings Localization { get; } = new();
    public PayrollDefaults Payroll { get; } = new();
    public NotificationSettings Notifications { get; } = new();
    public SecuritySettings Security { get; } = new();
    public List<IntegrationStatus> Integrations { get; } = SeedIntegrations();

    /// <summary>
    /// User-selected accent color override. The base ThemeService handles the
    /// dark/light mode. The accent is stored here so the settings page can
    /// drive both surfaces from one place.
    /// </summary>
    public string AccentColor { get; set; } = "#1D9E75";

    public event Action? OnChange;

    public void NotifyChanged() => OnChange?.Invoke();

    public void ResetToDefaults()
    {
        // For demo simplicity, just re-seed organization-defining fields back to
        // baseline values. Real implementations would track a baseline snapshot.
        Organization.LegalName = "";
        Organization.TradeName = "";
        Organization.Address = "";
        Organization.City = "";
        Organization.Province = "";
        Organization.PostalCode = "";
        Organization.Phone = "";
        Organization.Email = "";
        Organization.Website = "";
        Organization.Tin = "";
        Organization.RdoCode = "";
        Organization.SssEmployerNo = "";
        Organization.PhilHealthEmployerNo = "";
        Organization.PagibigEmployerNo = "";
        Organization.DoleEstablishmentNo = "";

        Localization.Timezone = "Asia/Manila";
        Localization.DateFormat = DateFormat.UsLong;
        Localization.CurrencyCode = "PHP";
        Localization.CurrencySymbol = "₱";
        Localization.WeekStart = WeekStart.Sunday;
        Localization.FiscalYearStartMonth = 1;

        AccentColor = "#1D9E75";
        NotifyChanged();
    }

    private static OrganizationProfile SeedOrganization() => new()
    {
        // Sensible PH-flavoured defaults so a fresh page isn't a wall of empty fields.
        // The user can override these via the Settings page.
        LegalName = "Giwu HR Solutions, Inc.",
        TradeName = "Giwu HR",
        Address = "23F BPI-Philam Tower, 6811 Ayala Ave",
        City = "Makati City",
        Province = "Metro Manila",
        PostalCode = "1226",
        Phone = "+63 2 8888 1234",
        Email = "info@giwu.com",
        Website = "https://giwu.com",
        Tin = "001-234-567-000",
        RdoCode = "044",
        SssEmployerNo = "03-9876543-2",
        PhilHealthEmployerNo = "00-000123456-7",
        PagibigEmployerNo = "1234-5678-9012",
        DoleEstablishmentNo = "DOLE-NCR-2018-0001",
    };

    private static List<IntegrationStatus> SeedIntegrations() => new()
    {
        new() { Name="Biometric Time Capture", Provider="ZKTeco K40", Category="Time",
                Description="Sync clock-in/out events from biometric devices into Attendance.",
                IsConnected=true, ConnectedOn=DateTime.Today.AddYears(-1), Icon="Fingerprint" },
        new() { Name="QuickBooks", Provider="Intuit", Category="Accounting",
                Description="Export approved payroll runs as journal entries.",
                IsConnected=false, Icon="Calculate" },
        new() { Name="Email Provider", Provider="SendGrid", Category="Email",
                Description="Send payslips, notifications, and scheduled reports.",
                IsConnected=true, ConnectedOn=DateTime.Today.AddMonths(-8), Icon="Email" },
        new() { Name="Single Sign-On", Provider="Microsoft Entra ID", Category="Identity",
                Description="Allow employees to sign in with company Microsoft accounts.",
                IsConnected=false, Icon="Key" },
        new() { Name="File Storage", Provider="AWS S3", Category="Storage",
                Description="Store contracts, IDs, and exported reports.",
                IsConnected=true, ConnectedOn=DateTime.Today.AddMonths(-6), Icon="CloudQueue" },
        new() { Name="Slack Notifications", Provider="Slack", Category="Email",
                Description="Send approval reminders and alerts to a Slack channel.",
                IsConnected=false, Icon="Forum" },
    };
}
