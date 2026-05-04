using FluentValidation;
using Giwu.Api.Common;
using Giwu.Api.Middleware;
using Giwu.Application.Common;
using Giwu.Application.Employees.Commands;
using Giwu.Application.Employees.Queries;
using Giwu.Contracts.Employees;
using Giwu.Domain.Identity;
using MediatR;

namespace Giwu.Api.Endpoints.Employees;

public sealed class GetEmployeeEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/employees/{id:guid}", Handle)
           .RequireAuthorization(Permissions.Employees.View)
           .WithTags("Employees");

    private static async Task<IResult> Handle(Guid id, IMediator m, CancellationToken ct) =>
        (await m.Send(new GetEmployeeQuery(id), ct)).ToHttp();
}

public sealed class UpdateEmployeeEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPut("/api/employees/{id:guid}", Handle)
           .RequireAuthorization(Permissions.Employees.Manage)
           .WithTags("Employees");

    private static async Task<IResult> Handle(Guid id, UpdateEmployeeRequest body, IMediator m, CancellationToken ct)
    {
        try { return (await m.Send(new UpdateEmployeeCommand(id, body), ct)).ToHttp(); }
        catch (ValidationException ex)
        {
            return Results.ValidationProblem(ex.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
        }
    }
}

public sealed class DeleteEmployeeEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapDelete("/api/employees/{id:guid}", Handle)
           .RequireAuthorization(Permissions.Employees.Manage)
           .WithTags("Employees");

    private static async Task<IResult> Handle(Guid id, IMediator m, CancellationToken ct) =>
        (await m.Send(new DeleteEmployeeCommand(id), ct)).ToHttp();
}
