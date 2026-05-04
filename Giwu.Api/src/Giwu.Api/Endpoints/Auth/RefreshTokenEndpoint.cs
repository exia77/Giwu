using Giwu.Api.Common;
using Giwu.Api.Middleware;
using Giwu.Application.Auth.Refresh;
using Giwu.Contracts.Auth;
using MediatR;

namespace Giwu.Api.Endpoints.Auth;

public sealed class RefreshTokenEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/api/auth/refresh", Handle)
           .AllowAnonymous()
           .WithTags("Auth")
           .WithSummary("Exchange a refresh token for a new access + refresh pair");

    private static async Task<IResult> Handle(
        RefreshRequest body, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new RefreshTokenCommand(body.RefreshToken), ct);
        return result.ToHttp();
    }
}
