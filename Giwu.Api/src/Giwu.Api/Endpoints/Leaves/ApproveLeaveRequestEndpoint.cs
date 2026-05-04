using FluentValidation;
using Giwu.Api.Common;
using Giwu.Api.Middleware;
using Giwu.Application.Leaves.Commands;
using Giwu.Contracts.Leaves;
using Giwu.Domain.Identity;
using MediatR;

namespace Giwu.Api.Endpoints.Leaves;

public sealed class ApproveLeaveRequestEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/api/leave-requests/{id:guid}/approve", Handle)
           .RequireAuthorization(Permissions.Leaves.Approve)
           .WithTags("Leaves")
           .WithSummary("Approve a pending leave request");

    private static async Task<IResult> Handle(
        Guid id, ApproveLeaveRequest body, IMediator mediator, CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(new ApproveLeaveRequestCommand(id, body.Note), ct);
            return result.ToHttp();
        }
        catch (ValidationException ex)
        {
            return Results.ValidationProblem(ex.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
        }
    }
}

public sealed class RejectLeaveRequestEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/api/leave-requests/{id:guid}/reject", Handle)
           .RequireAuthorization(Permissions.Leaves.Approve)
           .WithTags("Leaves")
           .WithSummary("Reject a pending leave request (release held balance)");

    private static async Task<IResult> Handle(
        Guid id, RejectLeaveRequest body, IMediator mediator, CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(new RejectLeaveRequestCommand(id, body.Note), ct);
            return result.ToHttp();
        }
        catch (ValidationException ex)
        {
            return Results.ValidationProblem(ex.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
        }
    }
}
