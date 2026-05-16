using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Giwu.Contracts.Auth;

namespace Giwu.HRMS.Hybrid.Services.Auth;

/// <summary>
/// Strongly-typed wrapper for the Auth endpoints exposed by Giwu.Api.
/// Registered via <c>AddHttpClient&lt;IAuthApi, AuthApiClient&gt;</c>.
/// </summary>
public interface IAuthApi
{
    Task<AuthCallResult<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<AuthCallResult<LoginResponse>> RefreshAsync(RefreshRequest request, CancellationToken ct = default);
    Task<AuthCallResult<UserMeDto>> MeAsync(CancellationToken ct = default);
    Task<AuthCallResult<bool>> LogoutAsync(LogoutRequest request, CancellationToken ct = default);

    Task<AuthCallResult<bool>> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct = default);
    Task<AuthCallResult<bool>> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default);

    Task<AuthCallResult<LoginResponse>> GoogleSignInAsync(GoogleSignInRequest request, CancellationToken ct = default);
    Task<AuthCallResult<GoogleConfigDto>> GoogleConfigAsync(CancellationToken ct = default);

    /// <summary>Kicks off the server-side OAuth flow — returns the session id + Google auth URL.</summary>
    Task<AuthCallResult<GoogleOAuthStartDto>> GoogleOAuthStartAsync(CancellationToken ct = default);

    /// <summary>Polls the OAuth session — returns Ready=true when the user has finished signing in.</summary>
    Task<AuthCallResult<GoogleOAuthPollDto>> GoogleOAuthPollAsync(string sessionId, CancellationToken ct = default);
}

public sealed record AuthCallResult<T>(bool Success, T? Value, string? ErrorMessage)
{
    public static AuthCallResult<T> Ok(T value) => new(true, value, null);
    public static AuthCallResult<T> Fail(string error) => new(false, default, error);
}

public sealed class AuthApiClient(HttpClient http) : IAuthApi
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public Task<AuthCallResult<LoginResponse>> LoginAsync(LoginRequest req, CancellationToken ct = default) =>
        PostAsync<LoginRequest, LoginResponse>("/api/auth/login", req, ct);

    public Task<AuthCallResult<LoginResponse>> RefreshAsync(RefreshRequest req, CancellationToken ct = default) =>
        PostAsync<RefreshRequest, LoginResponse>("/api/auth/refresh", req, ct);

    public Task<AuthCallResult<LoginResponse>> GoogleSignInAsync(GoogleSignInRequest req, CancellationToken ct = default) =>
        PostAsync<GoogleSignInRequest, LoginResponse>("/api/auth/google", req, ct);

    public async Task<AuthCallResult<bool>> ForgotPasswordAsync(ForgotPasswordRequest req, CancellationToken ct = default)
    {
        try
        {
            var res = await http.PostAsJsonAsync("/api/auth/forgot-password", req, JsonOpts, ct);
            return res.IsSuccessStatusCode
                ? AuthCallResult<bool>.Ok(true)
                : AuthCallResult<bool>.Fail($"HTTP {(int)res.StatusCode}");
        }
        catch (HttpRequestException)
        {
            return AuthCallResult<bool>.Fail("Cannot reach the server. Check your connection.");
        }
        catch (Exception ex)
        {
            return AuthCallResult<bool>.Fail(ex.Message);
        }
    }

    public async Task<AuthCallResult<bool>> ResetPasswordAsync(ResetPasswordRequest req, CancellationToken ct = default)
    {
        try
        {
            var res = await http.PostAsJsonAsync("/api/auth/reset-password", req, JsonOpts, ct);
            if (res.IsSuccessStatusCode) return AuthCallResult<bool>.Ok(true);

            var msg = res.StatusCode switch
            {
                System.Net.HttpStatusCode.NotFound   => "Invalid or expired reset token",
                System.Net.HttpStatusCode.BadRequest => await TryReadProblem(res, ct),
                _                                    => $"HTTP {(int)res.StatusCode}",
            };
            return AuthCallResult<bool>.Fail(msg);
        }
        catch (HttpRequestException)
        {
            return AuthCallResult<bool>.Fail("Cannot reach the server. Check your connection.");
        }
        catch (Exception ex)
        {
            return AuthCallResult<bool>.Fail(ex.Message);
        }
    }

    public async Task<AuthCallResult<GoogleConfigDto>> GoogleConfigAsync(CancellationToken ct = default)
    {
        try
        {
            var res = await http.GetAsync("/api/auth/google/config", ct);
            if (!res.IsSuccessStatusCode)
                return AuthCallResult<GoogleConfigDto>.Fail($"HTTP {(int)res.StatusCode}");

            var dto = await res.Content.ReadFromJsonAsync<GoogleConfigDto>(JsonOpts, ct);
            return dto is null
                ? AuthCallResult<GoogleConfigDto>.Fail("Empty response body")
                : AuthCallResult<GoogleConfigDto>.Ok(dto);
        }
        catch (Exception ex)
        {
            return AuthCallResult<GoogleConfigDto>.Fail(ex.Message);
        }
    }

    public async Task<AuthCallResult<GoogleOAuthStartDto>> GoogleOAuthStartAsync(CancellationToken ct = default)
    {
        try
        {
            var res = await http.GetAsync("/api/auth/google/start", ct);
            if (!res.IsSuccessStatusCode)
                return AuthCallResult<GoogleOAuthStartDto>.Fail($"HTTP {(int)res.StatusCode}");

            var dto = await res.Content.ReadFromJsonAsync<GoogleOAuthStartDto>(JsonOpts, ct);
            return dto is null
                ? AuthCallResult<GoogleOAuthStartDto>.Fail("Empty response body")
                : AuthCallResult<GoogleOAuthStartDto>.Ok(dto);
        }
        catch (Exception ex)
        {
            return AuthCallResult<GoogleOAuthStartDto>.Fail(ex.Message);
        }
    }

    public async Task<AuthCallResult<GoogleOAuthPollDto>> GoogleOAuthPollAsync(string sessionId, CancellationToken ct = default)
    {
        try
        {
            var res = await http.GetAsync($"/api/auth/google/poll?session={Uri.EscapeDataString(sessionId)}", ct);
            if (!res.IsSuccessStatusCode)
                return AuthCallResult<GoogleOAuthPollDto>.Fail($"HTTP {(int)res.StatusCode}");

            var dto = await res.Content.ReadFromJsonAsync<GoogleOAuthPollDto>(JsonOpts, ct);
            return dto is null
                ? AuthCallResult<GoogleOAuthPollDto>.Fail("Empty response body")
                : AuthCallResult<GoogleOAuthPollDto>.Ok(dto);
        }
        catch (Exception ex)
        {
            return AuthCallResult<GoogleOAuthPollDto>.Fail(ex.Message);
        }
    }

    public async Task<AuthCallResult<UserMeDto>> MeAsync(CancellationToken ct = default)
    {
        try
        {
            var res = await http.GetAsync("/api/auth/me", ct);
            return res.IsSuccessStatusCode
                ? AuthCallResult<UserMeDto>.Ok((await res.Content.ReadFromJsonAsync<UserMeDto>(JsonOpts, ct))!)
                : AuthCallResult<UserMeDto>.Fail($"HTTP {(int)res.StatusCode}");
        }
        catch (Exception ex)
        {
            return AuthCallResult<UserMeDto>.Fail(ex.Message);
        }
    }

    public async Task<AuthCallResult<bool>> LogoutAsync(LogoutRequest request, CancellationToken ct = default)
    {
        try
        {
            var res = await http.PostAsJsonAsync("/api/auth/logout", request, JsonOpts, ct);
            return res.IsSuccessStatusCode
                ? AuthCallResult<bool>.Ok(true)
                : AuthCallResult<bool>.Fail($"HTTP {(int)res.StatusCode}");
        }
        catch (HttpRequestException)
        {
            return AuthCallResult<bool>.Fail("Cannot reach the server. Check your connection.");
        }
        catch (Exception ex)
        {
            return AuthCallResult<bool>.Fail(ex.Message);
        }
    }

    private async Task<AuthCallResult<TResp>> PostAsync<TReq, TResp>(
        string path, TReq body, CancellationToken ct)
    {
        try
        {
            var res = await http.PostAsJsonAsync(path, body, JsonOpts, ct);
            if (res.IsSuccessStatusCode)
            {
                var value = await res.Content.ReadFromJsonAsync<TResp>(JsonOpts, ct);
                return value is null
                    ? AuthCallResult<TResp>.Fail("Empty response body")
                    : AuthCallResult<TResp>.Ok(value);
            }

            var msg = res.StatusCode switch
            {
                HttpStatusCode.NotFound       => "Invalid credentials",
                HttpStatusCode.Forbidden      => "Account locked or disabled",
                HttpStatusCode.Unauthorized   => "Authentication required",
                HttpStatusCode.BadRequest     => await TryReadProblem(res, ct),
                _                             => $"HTTP {(int)res.StatusCode}",
            };
            return AuthCallResult<TResp>.Fail(msg);
        }
        catch (HttpRequestException)
        {
            return AuthCallResult<TResp>.Fail("Cannot reach the server. Check your connection.");
        }
        catch (Exception ex)
        {
            return AuthCallResult<TResp>.Fail(ex.Message);
        }
    }

    private static async Task<string> TryReadProblem(HttpResponseMessage res, CancellationToken ct)
    {
        try
        {
            var problem = await res.Content.ReadFromJsonAsync<ValidationProblemDetails>(JsonOpts, ct);
            if (problem?.Errors is { Count: > 0 } errs)
                return string.Join("; ", errs.SelectMany(kv => kv.Value));
        }
        catch { /* fall through */ }
        return "Validation failed";
    }

    private sealed class ValidationProblemDetails
    {
        public Dictionary<string, string[]>? Errors { get; set; }
    }
}
