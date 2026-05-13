namespace Giwu.Application.Common;

/// <summary>
/// Cross-cutting URL settings (e.g. password-reset links embedded in outbound emails).
/// </summary>
public sealed class AppUrlOptions
{
    public string BaseUrl { get; set; } = "";
}
