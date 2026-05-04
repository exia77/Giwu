using Giwu.Api.Common;
using Giwu.Api.Middleware;
using Giwu.Application.Employees.Queries;
using Giwu.Domain.Identity;
using MediatR;

namespace Giwu.Api.Endpoints.Employees;

public sealed class ListEmployeesEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/employees", Handle)
           .RequireAuthorization(Permissions.Employees.View)
           .WithTags("Employees")
           .WithSummary("List employees (paged, optional search)");

    private static async Task<IResult> Handle(
        IMediator mediator,
        int page = 1, int pageSize = 25, string? search = null,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new ListEmployeesQuery(page, pageSize, search), ct);
        return result.ToHttp();
    }
}
