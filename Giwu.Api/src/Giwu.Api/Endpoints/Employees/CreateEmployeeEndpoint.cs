using FluentValidation;
using Giwu.Api.Common;
using Giwu.Api.Middleware;
using Giwu.Application.Employees.Commands;
using Giwu.Contracts.Employees;
using Giwu.Domain.Identity;
using MediatR;

namespace Giwu.Api.Endpoints.Employees;

public sealed class CreateEmployeeEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/api/employees", Handle)
           .RequireAuthorization(Permissions.Employees.Manage)
           .WithTags("Employees")
           .WithSummary("Create a new employee record");

    private static async Task<IResult> Handle(
        CreateEmployeeRequest body, IMediator mediator, CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(new CreateEmployeeCommand(body), ct);
            return result.Kind == Giwu.Application.Common.ResultKind.Success
                ? Results.Created($"/api/employees/{result.Value!.Id}", result.Value)
                : result.ToHttp();
        }
        catch (ValidationException ex)
        {
            var errors = ex.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            return Results.ValidationProblem(errors);
        }
    }
}
