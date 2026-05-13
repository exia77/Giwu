using FluentValidation;
using Giwu.Api.Common;
using Giwu.Api.Middleware;
using Giwu.Application.Auth.Google;
using Giwu.Contracts.Auth;
using Giwu.Infrastructure.Auth;
using MediatR;
using Microsoft.Extensions.Options;

namespace Giwu.Api.Endpoints.Auth;

public sealed class GoogleSignInEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/api/auth/google", Handle)
           .AllowAnonymous()
           .WithTags("Auth")
           .WithSummary("Exchange a Google ID token for a JWT pair");

    private static async Task<IResult> Handle(
        GoogleSignInRequest body, IMediator mediator, CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(new GoogleSignInCommand(body.IdToken), ct);
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

public sealed class GoogleConfigEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/auth/google/config", Handle)
           .AllowAnonymous()
           .WithTags("Auth")
           .WithSummary("Returns the Google OAuth client ID for the frontend to render the Google button");

    private static IResult Handle(IOptions<GoogleOptions> opts) =>
        Results.Ok(new GoogleConfigDto(opts.Value.ClientId));
}
