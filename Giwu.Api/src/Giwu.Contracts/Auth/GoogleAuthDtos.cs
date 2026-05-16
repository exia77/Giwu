namespace Giwu.Contracts.Auth;

public sealed record GoogleSignInRequest(string IdToken);

public sealed record GoogleConfigDto(string ClientId);

/// <summary>Response from GET /api/auth/google/start — what the MAUI client needs to drive the flow.</summary>
public sealed record GoogleOAuthStartDto(string SessionId, string AuthorizationUrl);

/// <summary>Response from GET /api/auth/google/poll — either still pending or done.</summary>
public sealed record GoogleOAuthPollDto(bool Ready, LoginResponse? Login, string? ErrorMessage);
