using Giwu.Api.Common;
using Giwu.Api.Middleware;
using Giwu.Application.Leaves.Queries;
using Giwu.Domain.Leaves;
using MediatR;

namespace Giwu.Api.Endpoints.Leaves;

public sealed class ListLeaveRequestsEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/leave-requests", Handle)
           .RequireAuthorization()      // any authenticated user; mineOnly=true scopes to self
           .WithTags("Leaves")
           .WithSummary("List leave requests (paged, filterable by status / employee / mine)");

    private static async Task<IResult> Handle(
        IMediator mediator,
        int page = 1, int pageSize = 25,
        LeaveRequestStatus? status = null,
        Guid? employeeId = null,
        bool mineOnly = false,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(
            new ListLeaveRequestsQuery(page, pageSize, status, employeeId, mineOnly), ct);
        return result.ToHttp();
    }
}
