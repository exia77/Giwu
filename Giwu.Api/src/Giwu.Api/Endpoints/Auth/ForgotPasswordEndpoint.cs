using FluentValidation;
using Giwu.Api.Common;
using Giwu.Api.Middleware;
using Giwu.Application.Auth.PasswordReset;
using Giwu.Contracts.Auth;
using MediatR;

namespace Giwu.Api.Endpoints.Auth;

public sealed class ForgotPasswordEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/api/auth/forgot-password", Handle)
           .AllowAnonymous()
           .WithTags("Auth")
           .WithSummary("Send a password reset email (always returns 200 to prevent enumeration)");

    private static async Task<IResult> Handle(
        ForgotPasswordRequest body, IMediator mediator, CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(new ForgotPasswordCommand(body.Email), ct);
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

public sealed class ResetPasswordEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/api/auth/reset-password", Handle)
           .AllowAnonymous()
           .WithTags("Auth")
           .WithSummary("Reset password using an emailed token");

    private static async Task<IResult> Handle(
        ResetPasswordRequest body, IMediator mediator, CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(
                new ResetPasswordCommand(body.Email, body.Token, body.NewPassword), ct);
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
