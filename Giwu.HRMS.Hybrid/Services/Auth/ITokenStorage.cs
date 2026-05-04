namespace Giwu.HRMS.Hybrid.Services.Auth;

public interface ITokenStorage
{
    Task<StoredTokens?> GetAsync();
    Task SetAsync(StoredTokens tokens);
    Task ClearAsync();
}

public sealed record StoredTokens(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt,
    string Email);
