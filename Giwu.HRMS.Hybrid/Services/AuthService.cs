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

    /// <summary>Cached EmployeeId from the most recent login/refresh/me response.</summary>
    public Guid? EmployeeId { get; private set; }

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

    public async Task<string?> SignInWithGoogleSystemBrowserAsync(CancellationToken ct = default)
    {
        var start = await api.GoogleOAuthStartAsync(ct);
        if (!start.Success || start.Value is null)
            return start.ErrorMessage ?? "Could not start Google sign-in.";

        // Open Google's auth page in the user's default browser. The user signs in
        // there; our /api/auth/google/callback completes the flow on the server.
        try
        {
            await Microsoft.Maui.ApplicationModel.Browser.Default.OpenAsync(
                start.Value.AuthorizationUrl,
                Microsoft.Maui.ApplicationModel.BrowserLaunchMode.External);
        }
        catch (Exception ex)
        {
            return $"Could not open the browser: {ex.Message}";
        }

        // Poll the server until the user finishes (or cancels, or 2 minutes pass).
        var sessionId = start.Value.SessionId;
        var deadline = DateTimeOffset.UtcNow.AddMinutes(2);
        while (DateTimeOffset.UtcNow < deadline)
        {
            try { await Task.Delay(TimeSpan.FromSeconds(2), ct); }
            catch (TaskCanceledException) { return "Sign-in cancelled."; }

            var poll = await api.GoogleOAuthPollAsync(sessionId, ct);
            if (!poll.Success || poll.Value is null) continue; // transient network blip — keep trying
            if (!poll.Value.Ready) continue;                   // user still finishing in the browser

            if (poll.Value.Login is null)
                return poll.Value.ErrorMessage ?? "Google sign-in failed.";

            return await CompleteLoginAsync(
                new AuthCallResult<LoginResponse>(true, poll.Value.Login, null),
                "Google sign-in failed");
        }

        return "Sign-in timed out. Try again.";
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

        EmployeeId = resp.User.EmployeeId;
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
                EmployeeId = me.Value.EmployeeId;
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
            EmployeeId = r.User.EmployeeId;
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
        EmployeeId = null;
        IsLoggedIn = false;
        OnChange?.Invoke();
    }
}
