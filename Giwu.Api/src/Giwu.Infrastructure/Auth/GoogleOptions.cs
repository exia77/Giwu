namespace Giwu.Infrastructure.Auth;

public sealed class GoogleOptions
{
    /// <summary>
    /// Google OAuth Web client ID (e.g. "1234567890-abc.apps.googleusercontent.com").
    /// Required for Google sign-in to be available.
    /// </summary>
    public string ClientId { get; set; } = "";
}
