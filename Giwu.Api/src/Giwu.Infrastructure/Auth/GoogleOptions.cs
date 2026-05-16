namespace Giwu.Infrastructure.Auth;

public sealed class GoogleOptions
{
    /// <summary>
    /// Google OAuth Web client ID (e.g. "1234567890-abc.apps.googleusercontent.com").
    /// Required for Google sign-in to be available.
    /// </summary>
    public string ClientId { get; set; } = "";

    /// <summary>
    /// OAuth Web-client secret. Required for the system-browser code-exchange flow
    /// used by the MAUI Hybrid client (the GIS popup flow doesn't need it).
    /// </summary>
    public string ClientSecret { get; set; } = "";

    /// <summary>
    /// Where Google should redirect after the user authenticates. Must match one
    /// of the Authorized redirect URIs registered on the OAuth client.
    /// Example: http://localhost:5080/api/auth/google/callback
    /// </summary>
    public string RedirectUri { get; set; } = "";
}
