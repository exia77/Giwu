using FluentValidation;
using Giwu.Api.Common;
using Giwu.Api.Middleware;
using Giwu.Application.Common;
using Giwu.Application.Leaves.LeaveTypes;
using Giwu.Contracts.Leaves;
using Giwu.Domain.Identity;
using MediatR;

namespace Giwu.Api.Endpoints.Leaves;

public sealed class ListLeaveTypesEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/leave-types", Handle)
           .RequireAuthorization()
           .WithTags("Leaves");

    private static async Task<IResult> Handle(IMediator m, bool includeInactive = false, CancellationToken ct = default) =>
        (await m.Send(new ListLeaveTypesQuery(includeInactive), ct)).ToHttp();
}

public sealed class CreateLeaveTypeEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/api/leave-types", Handle)
           .RequireAuthorization(Permissions.Leaves.Manage)
           .WithTags("Leaves");

    private static async Task<IResult> Handle(CreateLeaveTypeRequest body, IMediator m, CancellationToken ct)
    {
        try
        {
            var r = await m.Send(new CreateLeaveTypeCommand(body), ct);
            return r.Kind == ResultKind.Success
                ? Results.Created($"/api/leave-types/{r.Value!.Id}", r.Value)
                : r.ToHttp();
        }
        catch (ValidationException ex) { return ToProblem(ex); }
    }

    internal static IResult ToProblem(ValidationException ex) =>
        Results.ValidationProblem(ex.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
}

public sealed class UpdateLeaveTypeEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPut("/api/leave-types/{id:guid}", Handle)
           .RequireAuthorization(Permissions.Leaves.Manage)
           .WithTags("Leaves");

    private static async Task<IResult> Handle(Guid id, UpdateLeaveTypeRequest body, IMediator m, CancellationToken ct)
    {
        try { return (await m.Send(new UpdateLeaveTypeCommand(id, body), ct)).ToHttp(); }
        catch (ValidationException ex) { return CreateLeaveTypeEndpoint.ToProblem(ex); }
    }
}

public sealed class DeleteLeaveTypeEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapDelete("/api/leave-types/{id:guid}", Handle)
           .RequireAuthorization(Permissions.Leaves.Manage)
           .WithTags("Leaves");

    private static async Task<IResult> Handle(Guid id, IMediator m, CancellationToken ct) =>
        (await m.Send(new DeleteLeaveTypeCommand(id), ct)).ToHttp();
}
