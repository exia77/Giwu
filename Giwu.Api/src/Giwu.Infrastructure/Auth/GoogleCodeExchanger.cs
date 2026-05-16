using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Giwu.Application.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Giwu.Infrastructure.Auth;

/// <summary>
/// Calls Google's OAuth token endpoint to swap an authorization code for an
/// id_token. Uses the standard server-side Web flow (client_id + client_secret
/// + code + redirect_uri).
/// </summary>
public sealed class GoogleCodeExchanger : IGoogleCodeExchanger
{
    private const string TokenUrl = "https://oauth2.googleapis.com/token";

    private readonly HttpClient _http;
    private readonly GoogleOptions _opts;
    private readonly ILogger<GoogleCodeExchanger> _log;

    public GoogleCodeExchanger(
        HttpClient http,
        IOptions<GoogleOptions> opts,
        ILogger<GoogleCodeExchanger> log)
    {
        _http = http;
        _opts = opts.Value;
        _log = log;
    }

    public async Task<string?> ExchangeCodeForIdTokenAsync(string code, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_opts.ClientSecret) || string.IsNullOrEmpty(_opts.RedirectUri))
        {
            _log.LogWarning("Google code exchange skipped — ClientSecret or RedirectUri not configured.");
            return null;
        }

        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["code"]          = code,
            ["client_id"]     = _opts.ClientId,
            ["client_secret"] = _opts.ClientSecret,
            ["redirect_uri"]  = _opts.RedirectUri,
            ["grant_type"]    = "authorization_code",
        });

        try
        {
            var res = await _http.PostAsync(TokenUrl, form, ct);
            if (!res.IsSuccessStatusCode)
            {
                var body = await res.Content.ReadAsStringAsync(ct);
                _log.LogWarning("Google token exchange failed ({Status}): {Body}", (int)res.StatusCode, body);
                return null;
            }

            var token = await res.Content.ReadFromJsonAsync<GoogleTokenResponse>(cancellationToken: ct);
            return token?.IdToken;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Google token exchange threw.");
            return null;
        }
    }

    private sealed class GoogleTokenResponse
    {
        [JsonPropertyName("id_token")]      public string? IdToken { get; set; }
        [JsonPropertyName("access_token")]  public string? AccessToken { get; set; }
        [JsonPropertyName("expires_in")]    public int? ExpiresIn { get; set; }
        [JsonPropertyName("refresh_token")] public string? RefreshToken { get; set; }
        [JsonPropertyName("scope")]         public string? Scope { get; set; }
        [JsonPropertyName("token_type")]    public string? TokenType { get; set; }
    }
}
