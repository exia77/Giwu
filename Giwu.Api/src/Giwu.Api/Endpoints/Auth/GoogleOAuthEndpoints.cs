using Giwu.Api.Common;
using Giwu.Application.Auth.Google;
using Giwu.Application.Common;
using Giwu.Contracts.Auth;
using MediatR;

namespace Giwu.Api.Endpoints.Auth;

/// <summary>
/// MAUI Hybrid clients can't use the GIS popup (the WebView origin is rejected
/// by Google's allow-list), so they kick off a server-side OAuth flow instead.
/// The user's system browser does the auth dance; the MAUI app polls back here
/// for the resulting JWT keyed by a short-lived session id.
/// </summary>
public sealed class GoogleOAuthStartEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/auth/google/start", Handle)
           .AllowAnonymous()
           .WithTags("Auth")
           .WithSummary("Returns a session id + Google authorization URL for the system-browser flow");

    private static IResult Handle(IGoogleOAuthFlow flow)
    {
        var s = flow.Begin();
        return Results.Ok(new GoogleOAuthStartDto(s.SessionId, s.AuthorizationUrl));
    }
}

public sealed class GoogleOAuthCallbackEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/auth/google/callback", Handle)
           .AllowAnonymous()
           .WithTags("Auth")
           .WithSummary("OAuth callback — Google redirects here with the auth code");

    private static async Task<IResult> Handle(
        string? code,
        string? state,
        string? error,
        IGoogleOAuthFlow flow,
        IGoogleCodeExchanger exchanger,
        IMediator mediator,
        CancellationToken ct)
    {
        // Resolve session even on error so we can stash a useful message for the
        // MAUI app's poll. If state is bogus we just render an error page.
        var sessionId = string.IsNullOrEmpty(state) ? null : flow.ResolveSessionFromState(state);

        if (!string.IsNullOrEmpty(error))
        {
            if (sessionId is not null) flow.Fail(sessionId, $"Google sign-in cancelled: {error}");
            return HtmlPage("Sign-in cancelled", "You cancelled or denied access. You can close this tab.");
        }

        if (sessionId is null)
            return HtmlPage("Sign-in failed", "Unknown or expired sign-in session. Try again from the app.");

        if (string.IsNullOrEmpty(code))
        {
            flow.Fail(sessionId, "Google did not return an authorization code.");
            return HtmlPage("Sign-in failed", "Google did not return an authorization code.");
        }

        var idToken = await exchanger.ExchangeCodeForIdTokenAsync(code, ct);
        if (string.IsNullOrEmpty(idToken))
        {
            flow.Fail(sessionId, "Could not exchange the authorization code with Google.");
            return HtmlPage("Sign-in failed", "Server could not verify the Google response. Try again.");
        }

        var result = await mediator.Send(new GoogleSignInCommand(idToken), ct);
        if (result.Kind != ResultKind.Success || result.Value is null)
        {
            flow.Fail(sessionId, result.Message ?? "Google sign-in was rejected.");
            return HtmlPage("Sign-in failed", result.Message ?? "Your Google account isn't linked to an HRMS user.");
        }

        flow.Complete(sessionId, result.Value);
        return HtmlPage("You're signed in", "You can close this tab and return to the app.");
    }

    private static IResult HtmlPage(string title, string body)
    {
        var html = $@"<!doctype html><html><head>
<meta charset=""utf-8"" />
<title>{title}</title>
<style>
  body {{ font-family: -apple-system, system-ui, Segoe UI, sans-serif;
         display: flex; align-items: center; justify-content: center;
         min-height: 100vh; margin: 0;
         background: #0a1540; color: #fff; }}
  .card {{ background: rgba(255,255,255,.04); padding: 32px 40px; border-radius: 12px;
          max-width: 420px; text-align: center; }}
  h1 {{ margin: 0 0 12px; font-size: 18px; font-weight: 600; }}
  p  {{ margin: 0; opacity: .75; font-size: 14px; }}
</style></head>
<body><div class=""card""><h1>{System.Net.WebUtility.HtmlEncode(title)}</h1>
<p>{System.Net.WebUtility.HtmlEncode(body)}</p></div></body></html>";
        return Results.Content(html, "text/html");
    }
}

public sealed class GoogleOAuthPollEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/auth/google/poll", Handle)
           .AllowAnonymous()
           .WithTags("Auth")
           .WithSummary("MAUI client polls here until the system-browser auth flow finishes");

    private static IResult Handle(string session, IGoogleOAuthFlow flow)
    {
        var result = flow.TakeResult(session);
        if (result is null)
            return Results.Json(new GoogleOAuthPollDto(false, null, null), statusCode: 200);

        return Results.Ok(new GoogleOAuthPollDto(true, result.Response, result.ErrorMessage));
    }
}
