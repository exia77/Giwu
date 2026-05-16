using Giwu.Api.Common;
using Giwu.Api.Middleware;
using Giwu.Application.Auth.Queries;
using MediatR;

namespace Giwu.Api.Endpoints.Auth;

public sealed class MeEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/auth/me", Handle)
           .RequireAuthorization()
           .WithTags("Auth")
           .WithSummary("Returns the signed-in user's profile and permissions, freshly loaded from the database");

    // The handler hits the DB so role/permission changes show up on the next
    // /me call (typically: client app boot via TryRestoreAsync). The JWT's own
    // claims stay valid until expiry — only the client UI's view of the role
    // updates immediately.
    private static async Task<IResult> Handle(IMediator mediator, CancellationToken ct) =>
        (await mediator.Send(new GetCurrentUserQuery(), ct)).ToHttp();
}
