using System.Text.Json;
using Microsoft.Maui.Storage;

namespace Giwu.HRMS.Hybrid.Services.Auth;

/// <summary>
/// Persists JWT access + refresh tokens via MAUI <see cref="SecureStorage"/>
/// (Windows Credential Manager / Keychain on Mac / Keystore on Android etc.).
/// On platforms where SecureStorage is unavailable (rare in practice) falls
/// back to plain Preferences so the app keeps working.
/// </summary>
public sealed class SecureTokenStorage : ITokenStorage
{
    private const string Key = "giwu.auth.tokens";

    public async Task<StoredTokens?> GetAsync()
    {
        try
        {
            var json = await SecureStorage.Default.GetAsync(Key);
            return string.IsNullOrEmpty(json)
                ? null
                : JsonSerializer.Deserialize<StoredTokens>(json);
        }
        catch
        {
            var fallback = Preferences.Get(Key, (string?)null);
            return string.IsNullOrEmpty(fallback)
                ? null
                : JsonSerializer.Deserialize<StoredTokens>(fallback);
        }
    }

    public async Task SetAsync(StoredTokens tokens)
    {
        var json = JsonSerializer.Serialize(tokens);
        try
        {
            await SecureStorage.Default.SetAsync(Key, json);
        }
        catch
        {
            Preferences.Set(Key, json);
        }
    }

    public Task ClearAsync()
    {
        try { SecureStorage.Default.Remove(Key); }
        catch { /* best effort */ }
        Preferences.Remove(Key);
        return Task.CompletedTask;
    }
}
