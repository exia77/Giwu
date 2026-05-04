using Giwu.Api.Common;
using Giwu.Api.Middleware;
using Giwu.Application.Leaves.Commands;
using Giwu.Application.Leaves.Queries;
using MediatR;

namespace Giwu.Api.Endpoints.Leaves;

public sealed class GetLeaveRequestEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/leave-requests/{id:guid}", Handle)
           .RequireAuthorization()
           .WithTags("Leaves");

    private static async Task<IResult> Handle(Guid id, IMediator m, CancellationToken ct) =>
        (await m.Send(new GetLeaveRequestQuery(id), ct)).ToHttp();
}

public sealed class ListLeaveBalancesEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/leave-balances", Handle)
           .RequireAuthorization()
           .WithTags("Leaves");

    private static async Task<IResult> Handle(IMediator m, Guid? employeeId = null, int? year = null, CancellationToken ct = default) =>
        (await m.Send(new ListLeaveBalancesQuery(employeeId, year), ct)).ToHttp();
}

public sealed class CancelLeaveRequestEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/api/leave-requests/{id:guid}/cancel", Handle)
           .RequireAuthorization()
           .WithTags("Leaves");

    private static async Task<IResult> Handle(Guid id, IMediator m, CancellationToken ct) =>
        (await m.Send(new CancelLeaveRequestCommand(id), ct)).ToHttp();
}
