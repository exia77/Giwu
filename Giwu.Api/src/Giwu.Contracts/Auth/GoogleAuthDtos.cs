namespace Giwu.Contracts.Auth;

public sealed record GoogleSignInRequest(string IdToken);

public sealed record GoogleConfigDto(string ClientId);
