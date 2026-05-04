namespace Giwu.HRMS.Hybrid.Services;

public interface IThemeService
{
    bool IsDark { get; }
    string PrimaryColor { get; }
    string? LogoDataUrl { get; }
    string CompanyName { get; }
    event Action? OnChange;

    void SetDark(bool dark);
    void SetPrimary(string color);
    void SetLogo(string? dataUrl);
    void SetCompanyName(string name);
    string PrimaryAlpha(double alpha);
    string PrimaryDark();
}
