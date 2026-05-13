using Giwu.Contracts.Auth;
using Giwu.HRMS.Hybrid.Services.Auth;

namespace Giwu.HRMS.Hybrid.Services;

/// <summary>
/// Owns the "is anyone signed in" question. Delegates the network call to
/// <see cref="IAuthApi"/>, the token persistence to <see cref="ITokenStorage"/>,
/// and the user-profile state to <see cref="RoleAuthService"/>.
/// </summary>
public class AuthService(
    IAuthApi api,
    ITokenStorage tokens,
    RoleAuthService roles)
    : IAuthService
{
    public bool IsLoggedIn { get; private set; }

    public UserSession? CurrentUser => IsLoggedIn ? roles.CurrentUser : null;
    public string UserEmail => CurrentUser?.Email ?? string.Empty;
    public string UserName  => CurrentUser?.Name  ?? string.Empty;

    public event Action? OnChange;

    public async Task<string?> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        var result = await api.LoginAsync(new LoginRequest(email, password), ct);
        return await CompleteLoginAsync(result, "Login failed");
    }

    public async Task<string?> SignInWithGoogleAsync(string idToken, CancellationToken ct = default)
    {
        var result = await api.GoogleSignInAsync(new GoogleSignInRequest(idToken), ct);
        return await CompleteLoginAsync(result, "Google sign-in failed");
    }

    private async Task<string?> CompleteLoginAsync(AuthCallResult<LoginResponse> result, string defaultError)
    {
        if (!result.Success || result.Value is null)
            return result.ErrorMessage ?? defaultError;

        var resp = result.Value;
        await tokens.SetAsync(new StoredTokens(
            resp.AccessToken, resp.RefreshToken, resp.ExpiresAt, resp.User.Email));

        roles.SetCurrentUserFromApi(
            resp.User.Id.ToString(),
            resp.User.DisplayName,
            resp.User.Email,
            resp.User.Roles,
            resp.User.Permissions);

        IsLoggedIn = true;
        OnChange?.Invoke();
        return null;
    }

    public async Task TryRestoreAsync(CancellationToken ct = default)
    {
        var stored = await tokens.GetAsync();
        if (stored is null) return;

        // If access token still valid, hit /me to rehydrate; if expired, try refresh.
        if (stored.ExpiresAt > DateTimeOffset.UtcNow.AddMinutes(1))
        {
            var me = await api.MeAsync(ct);
            if (me.Success && me.Value is not null)
            {
                roles.SetCurrentUserFromApi(
                    me.Value.Id.ToString(), me.Value.DisplayName, me.Value.Email,
                    me.Value.Roles, me.Value.Permissions);
                IsLoggedIn = true;
                OnChange?.Invoke();
                return;
            }
        }

        var refreshed = await api.RefreshAsync(new RefreshRequest(stored.RefreshToken), ct);
        if (refreshed.Success && refreshed.Value is not null)
        {
            var r = refreshed.Value;
            await tokens.SetAsync(new StoredTokens(
                r.AccessToken, r.RefreshToken, r.ExpiresAt, r.User.Email));
            roles.SetCurrentUserFromApi(
                r.User.Id.ToString(), r.User.DisplayName, r.User.Email,
                r.User.Roles, r.User.Permissions);
            IsLoggedIn = true;
            OnChange?.Invoke();
            return;
        }

        // Stored tokens are unusable — clear them.
        await tokens.ClearAsync();
    }

    public async Task LogoutAsync(CancellationToken ct = default)
    {
        var stored = await tokens.GetAsync();
        if (stored is not null && !string.IsNullOrEmpty(stored.RefreshToken))
        {
            // Best-effort revoke on the server. Failures shouldn't block local sign-out.
            await api.LogoutAsync(new LogoutRequest(stored.RefreshToken), ct);
        }

        await tokens.ClearAsync();
        IsLoggedIn = false;
        OnChange?.Invoke();
    }
}
