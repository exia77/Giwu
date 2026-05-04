using System.Net.Http.Headers;

namespace Giwu.HRMS.Hybrid.Services.Auth;

/// <summary>
/// Delegating handler that attaches the current JWT to outgoing requests as
/// <c>Authorization: Bearer ...</c>. Reads the token from <see cref="ITokenStorage"/>
/// per call so a refresh elsewhere is picked up automatically.
/// </summary>
public sealed class AuthBearerHandler(ITokenStorage tokens) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken ct)
    {
        if (request.Headers.Authorization is null)
        {
            var stored = await tokens.GetAsync();
            if (stored is not null && !string.IsNullOrEmpty(stored.AccessToken))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", stored.AccessToken);
        }
        return await base.SendAsync(request, ct);
    }
}
