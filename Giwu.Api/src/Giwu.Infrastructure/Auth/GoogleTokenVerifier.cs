using Giwu.Application.Common;
using Google.Apis.Auth;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Giwu.Infrastructure.Auth;

public sealed class GoogleTokenVerifier(IOptions<GoogleOptions> opts, ILogger<GoogleTokenVerifier> log)
    : IGoogleTokenVerifier
{
    private readonly GoogleOptions _o = opts.Value;

    public async Task<GoogleUserInfo?> VerifyAsync(string idToken, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_o.ClientId))
        {
            log.LogWarning("Google sign-in attempted but Google:ClientId is not configured.");
            return null;
        }

        try
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { _o.ClientId },
            });

            return new GoogleUserInfo(
                Subject:       payload.Subject,
                Email:         payload.Email ?? string.Empty,
                EmailVerified: payload.EmailVerified,
                DisplayName:   payload.Name ?? payload.Email ?? string.Empty,
                PictureUrl:    payload.Picture);
        }
        catch (InvalidJwtException ex)
        {
            log.LogWarning(ex, "Invalid Google ID token.");
            return null;
        }
    }
}
