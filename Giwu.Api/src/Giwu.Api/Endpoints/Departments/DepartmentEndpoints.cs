using FluentValidation;
using Giwu.Api.Common;
using Giwu.Api.Middleware;
using Giwu.Application.Common;
using Giwu.Application.Departments.Commands;
using Giwu.Application.Departments.Queries;
using Giwu.Contracts.Departments;
using Giwu.Domain.Identity;
using MediatR;

namespace Giwu.Api.Endpoints.Departments;

public sealed class ListDepartmentsEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/departments", Handle)
           .RequireAuthorization(Permissions.Departments.View)
           .WithTags("Departments");

    private static async Task<IResult> Handle(IMediator m, bool includeInactive = false, CancellationToken ct = default) =>
        (await m.Send(new ListDepartmentsQuery(includeInactive), ct)).ToHttp();
}

public sealed class GetDepartmentEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/departments/{id:guid}", Handle)
           .RequireAuthorization(Permissions.Departments.View)
           .WithTags("Departments");

    private static async Task<IResult> Handle(Guid id, IMediator m, CancellationToken ct) =>
        (await m.Send(new GetDepartmentQuery(id), ct)).ToHttp();
}

public sealed class CreateDepartmentEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/api/departments", Handle)
           .RequireAuthorization(Permissions.Departments.Manage)
           .WithTags("Departments");

    private static async Task<IResult> Handle(CreateDepartmentRequest body, IMediator m, CancellationToken ct)
    {
        try
        {
            var r = await m.Send(new CreateDepartmentCommand(body), ct);
            return r.Kind == ResultKind.Success
                ? Results.Created($"/api/departments/{r.Value!.Id}", r.Value)
                : r.ToHttp();
        }
        catch (ValidationException ex) { return ToValidationProblem(ex); }
    }

    internal static IResult ToValidationProblem(ValidationException ex) =>
        Results.ValidationProblem(ex.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
}

public sealed class UpdateDepartmentEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPut("/api/departments/{id:guid}", Handle)
           .RequireAuthorization(Permissions.Departments.Manage)
           .WithTags("Departments");

    private static async Task<IResult> Handle(Guid id, UpdateDepartmentRequest body, IMediator m, CancellationToken ct)
    {
        try { return (await m.Send(new UpdateDepartmentCommand(id, body), ct)).ToHttp(); }
        catch (ValidationException ex) { return CreateDepartmentEndpoint.ToValidationProblem(ex); }
    }
}

public sealed class DeleteDepartmentEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapDelete("/api/departments/{id:guid}", Handle)
           .RequireAuthorization(Permissions.Departments.Manage)
           .WithTags("Departments");

    private static async Task<IResult> Handle(Guid id, IMediator m, CancellationToken ct) =>
        (await m.Send(new DeleteDepartmentCommand(id), ct)).ToHttp();
}
