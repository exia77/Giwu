namespace Giwu.Contracts.Settings;

public sealed record BrandingSettingsDto(
    string CompanyName,
    string LogoDataUrl,
    bool IsDark,
    string AccentColor);

public sealed record UpdateBrandingSettingsRequest(
    string CompanyName,
    string LogoDataUrl,
    bool IsDark,
    string AccentColor);
