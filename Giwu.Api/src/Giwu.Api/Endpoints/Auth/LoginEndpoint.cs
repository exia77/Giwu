using FluentValidation;
using Giwu.Api.Common;
using Giwu.Api.Middleware;
using Giwu.Application.Auth.Login;
using Giwu.Contracts.Auth;
using MediatR;

namespace Giwu.Api.Endpoints.Auth;

public sealed class LoginEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/api/auth/login", Handle)
           .AllowAnonymous()
           .WithTags("Auth")
           .WithSummary("Exchange email + password for a JWT pair");

    private static async Task<IResult> Handle(
        LoginRequest body, IMediator mediator, CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(new LoginCommand(body.Email, body.Password), ct);
            return result.ToHttp();
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
