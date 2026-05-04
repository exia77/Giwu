using FluentValidation;
using Giwu.Api.Common;
using Giwu.Api.Middleware;
using Giwu.Application.Leaves.Commands;
using Giwu.Contracts.Leaves;
using Giwu.Domain.Identity;
using MediatR;

namespace Giwu.Api.Endpoints.Leaves;

public sealed class FileLeaveRequestEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/api/leave-requests", Handle)
           .RequireAuthorization(Permissions.Leaves.FileSelf)
           .WithTags("Leaves")
           .WithSummary("File a new leave request for the signed-in employee");

    private static async Task<IResult> Handle(
        FileLeaveRequest body, IMediator mediator, CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(new FileLeaveRequestCommand(body), ct);
            return result.Kind == Giwu.Application.Common.ResultKind.Success
                ? Results.Created($"/api/leave-requests/{result.Value!.Id}", result.Value)
                : result.ToHttp();
        }
        catch (ValidationException ex)
        {
            return Results.ValidationProblem(ex.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
        }
    }
}
