namespace Giwu.HRMS.Hybrid.Components.Shared;

/// <summary>
/// Semantic tone for <see cref="StatCard"/> and other status-driven UI.
/// Top-level (not nested in StatCard) so call-sites can store it on local
/// records without colliding with the component name.
/// </summary>
public enum StatTone
{
    Neutral,
    Success,
    Warning,
    Danger,
    Info,
    Accent,
}
