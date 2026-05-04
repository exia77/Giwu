using Giwu.Api.Common;
using Giwu.Api.Middleware;
using Giwu.Application.Tenancy.Queries;
using MediatR;

namespace Giwu.Api.Endpoints.Tenancy;

public sealed class GetCurrentTenantEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/tenants/me", Handle)
           .RequireAuthorization()
           .WithTags("Tenancy")
           .WithSummary("Returns the current signed-in user's tenant profile");

    private static async Task<IResult> Handle(IMediator m, CancellationToken ct) =>
        (await m.Send(new GetCurrentTenantQuery(), ct)).ToHttp();
}
