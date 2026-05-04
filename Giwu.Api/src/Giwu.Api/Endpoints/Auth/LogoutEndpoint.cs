using Giwu.Api.Common;
using Giwu.Api.Middleware;
using Giwu.Application.Auth.Logout;
using Giwu.Contracts.Auth;
using MediatR;

namespace Giwu.Api.Endpoints.Auth;

public sealed class LogoutEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/api/auth/logout", Handle)
           .RequireAuthorization()
           .WithTags("Auth")
           .WithSummary("Revoke the supplied refresh token (or all sessions for the user)");

    private static async Task<IResult> Handle(LogoutRequest body, IMediator m, CancellationToken ct) =>
        (await m.Send(new LogoutCommand(body.RefreshToken, body.AllSessions), ct)).ToHttp();
}
