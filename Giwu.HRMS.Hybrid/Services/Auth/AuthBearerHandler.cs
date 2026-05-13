using System.Net;
using System.Net.Http.Headers;

namespace Giwu.HRMS.Hybrid.Services.Auth;

/// <summary>
/// Delegating handler that attaches the current JWT to outgoing requests as
/// <c>Authorization: Bearer ...</c>. Reads the token from <see cref="ITokenStorage"/>
/// per call so a refresh elsewhere is picked up automatically.
/// When the server returns 401, the stored tokens are cleared and the
/// <see cref="SessionSignal"/> is fired so the UI can redirect to login.
/// </summary>
public sealed class AuthBearerHandler(ITokenStorage tokens, SessionSignal signal) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken ct)
    {
        var attachedBearer = false;
        if (request.Headers.Authorization is null)
        {
            var stored = await tokens.GetAsync();
            if (stored is not null && !string.IsNullOrEmpty(stored.AccessToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", stored.AccessToken);
                attachedBearer = true;
            }
        }

        var response = await base.SendAsync(request, ct);

        if (response.StatusCode == HttpStatusCode.Unauthorized && attachedBearer)
        {
            await tokens.ClearAsync();
            signal.RaiseSessionExpired();
        }

        return response;
    }
}
