namespace Giwu.Contracts.Auth;

public sealed record LogoutRequest(string? RefreshToken = null, bool AllSessions = false);
