using Microsoft.Maui.Storage;

namespace Giwu.HRMS.Hybrid.Services;

public class ThemeService : IThemeService
{
    public bool IsDark { get; private set; }
    public string PrimaryColor { get; private set; } = "#667eea";
    public string? LogoDataUrl { get; private set; }
    public string CompanyName { get; private set; } = "GIWU HRMS";

    public event Action? OnChange;

    public ThemeService()
    {
        IsDark = Preferences.Get("theme_dark", false);
        PrimaryColor = Preferences.Get("theme_primary", "#667eea");
        LogoDataUrl = Preferences.Get("theme_logo", (string?)null);
        CompanyName = Preferences.Get("theme_company", "GIWU HRMS");
    }

    public void SetDark(bool dark)
    {
        IsDark = dark;
        Preferences.Set("theme_dark", dark);
        OnChange?.Invoke();
    }

    public void SetPrimary(string color)
    {
        PrimaryColor = color;
        Preferences.Set("theme_primary", color);
        OnChange?.Invoke();
    }

    public void SetLogo(string? dataUrl)
    {
        LogoDataUrl = dataUrl;
        if (dataUrl != null)
            Preferences.Set("theme_logo", dataUrl);
        else
            Preferences.Remove("theme_logo");
        OnChange?.Invoke();
    }

    public void SetCompanyName(string name)
    {
        CompanyName = name;
        Preferences.Set("theme_company", name);
        OnChange?.Invoke();
    }

    public string PrimaryAlpha(double alpha)
    {
        try
        {
            var hex = PrimaryColor.TrimStart('#');
            if (hex.Length == 6)
            {
                var r = Convert.ToInt32(hex[..2], 16);
                var g = Convert.ToInt32(hex[2..4], 16);
                var b = Convert.ToInt32(hex[4..6], 16);
                var a = alpha.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
                return $"rgba({r},{g},{b},{a})";
            }
        }
        catch { }
        return PrimaryColor;
    }

    public string PrimaryDark()
    {
        try
        {
            var hex = PrimaryColor.TrimStart('#');
            if (hex.Length == 6)
            {
                var r = Math.Max(0, Convert.ToInt32(hex[..2], 16) - 50);
                var g = Math.Max(0, Convert.ToInt32(hex[2..4], 16) - 50);
                var b = Math.Max(0, Convert.ToInt32(hex[4..6], 16) - 30);
                return $"#{r:X2}{g:X2}{b:X2}";
            }
        }
        catch { }
        return PrimaryColor;
    }
}
