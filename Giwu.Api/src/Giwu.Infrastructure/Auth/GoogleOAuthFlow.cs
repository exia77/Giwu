using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Giwu.Application.Auth.Google;
using Giwu.Contracts.Auth;
using Microsoft.Extensions.Options;

namespace Giwu.Infrastructure.Auth;

/// <summary>
/// In-memory implementation of <see cref="IGoogleOAuthFlow"/>. Holds pending
/// and completed sessions in two small concurrent dictionaries with a 5-minute
/// TTL. Single-instance only — swap for a Redis-backed store if the API ever
/// scales out horizontally.
/// </summary>
public sealed class GoogleOAuthFlow : IGoogleOAuthFlow
{
    private static readonly TimeSpan SessionTtl = TimeSpan.FromMinutes(5);

    // state token → (session id, expiresAt)
    private readonly ConcurrentDictionary<string, (string SessionId, DateTimeOffset ExpiresAt)> _pending = new();

    // session id → (result, expiresAt)
    private readonly ConcurrentDictionary<string, (GoogleOAuthResult Result, DateTimeOffset ExpiresAt)> _ready = new();

    private readonly GoogleOptions _opts;
    private readonly TimeProvider _clock;

    public GoogleOAuthFlow(IOptions<GoogleOptions> opts, TimeProvider clock)
    {
        _opts = opts.Value;
        _clock = clock;
    }

    public GoogleOAuthStart Begin()
    {
        SweepExpired();

        var sessionId = RandomToken(32);
        var state = RandomToken(32);
        var expires = _clock.GetUtcNow().Add(SessionTtl);
        _pending[state] = (sessionId, expires);

        var url = BuildAuthorizationUrl(state);
        return new GoogleOAuthStart(sessionId, url);
    }

    public string? ResolveSessionFromState(string state)
    {
        SweepExpired();
        if (string.IsNullOrEmpty(state)) return null;
        if (!_pending.TryRemove(state, out var pending)) return null;
        if (pending.ExpiresAt < _clock.GetUtcNow()) return null;
        return pending.SessionId;
    }

    public void Complete(string sessionId, LoginResponse response)
    {
        var expires = _clock.GetUtcNow().Add(SessionTtl);
        _ready[sessionId] = (new GoogleOAuthResult(response, null), expires);
    }

    public void Fail(string sessionId, string errorMessage)
    {
        var expires = _clock.GetUtcNow().Add(SessionTtl);
        _ready[sessionId] = (new GoogleOAuthResult(null, errorMessage), expires);
    }

    public GoogleOAuthResult? TakeResult(string sessionId)
    {
        SweepExpired();
        if (string.IsNullOrEmpty(sessionId)) return null;
        if (!_ready.TryRemove(sessionId, out var ready)) return null;
        if (ready.ExpiresAt < _clock.GetUtcNow()) return null;
        return ready.Result;
    }

    private string BuildAuthorizationUrl(string state)
    {
        // Standard Web-server OAuth flow — code response type, openid + email + profile
        // scopes give us the id_token we already know how to verify.
        var q = new Dictionary<string, string?>
        {
            ["client_id"]     = _opts.ClientId,
            ["redirect_uri"]  = _opts.RedirectUri,
            ["response_type"] = "code",
            ["scope"]         = "openid email profile",
            ["state"]         = state,
            ["access_type"]   = "online",
            ["prompt"]        = "select_account",
        };
        var qs = string.Join("&", q.Where(kv => !string.IsNullOrEmpty(kv.Value))
                                    .Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value!)}"));
        return $"https://accounts.google.com/o/oauth2/v2/auth?{qs}";
    }

    private void SweepExpired()
    {
        var now = _clock.GetUtcNow();
        foreach (var kv in _pending)
            if (kv.Value.ExpiresAt < now) _pending.TryRemove(kv.Key, out _);
        foreach (var kv in _ready)
            if (kv.Value.ExpiresAt < now) _ready.TryRemove(kv.Key, out _);
    }

    private static string RandomToken(int bytes)
    {
        var buf = new byte[bytes];
        RandomNumberGenerator.Fill(buf);
        // Base64url — URL-safe without padding shenanigans.
        return Convert.ToBase64String(buf)
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }
}
