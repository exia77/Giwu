using Giwu.Contracts.Auth;

namespace Giwu.Application.Auth.Google;

/// <summary>
/// Orchestrates the server-side OAuth flow used by the MAUI Hybrid client.
/// The client can't run the GIS popup (the WebView origin is unreachable in
/// Google's allow-list), so we do the whole dance server-side and have the
/// client poll for the resulting JWT keyed by a short-lived session id.
/// </summary>
public interface IGoogleOAuthFlow
{
    /// <summary>
    /// Reserves a polling session, returns the Google authorization URL the
    /// user's browser should visit and the session id the client must poll with.
    /// </summary>
    GoogleOAuthStart Begin();

    /// <summary>
    /// Validates the OAuth state token, finds the matching pending session,
    /// returns the session id so the callback endpoint can store the JWT against it.
    /// Null if the state is unknown or expired.
    /// </summary>
    string? ResolveSessionFromState(string state);

    /// <summary>Stores the issued JWT against a session id, ready for the client to poll.</summary>
    void Complete(string sessionId, LoginResponse response);

    /// <summary>Stores a user-facing error against a session id (e.g. user denied consent).</summary>
    void Fail(string sessionId, string errorMessage);

    /// <summary>
    /// Returns the stored result for a session id and removes it. Returns null
    /// if the session isn't ready yet (client should keep polling) or has expired.
    /// </summary>
    GoogleOAuthResult? TakeResult(string sessionId);
}

public sealed record GoogleOAuthStart(string SessionId, string AuthorizationUrl);

public sealed record GoogleOAuthResult(LoginResponse? Response, string? ErrorMessage);
