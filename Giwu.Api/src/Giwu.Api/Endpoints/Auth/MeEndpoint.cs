using Giwu.Api.Common;
using Giwu.Application.Common;
using Giwu.Contracts.Auth;

namespace Giwu.Api.Endpoints.Auth;

public sealed class MeEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/auth/me", Handle)
           .RequireAuthorization()
           .WithTags("Auth")
           .WithSummary("Returns the signed-in user's profile and permissions");

    private static IResult Handle(ICurrentUser user) =>
        user.IsAuthenticated
            ? Results.Ok(new UserMeDto(
                user.Id, user.TenantId, user.EmployeeId,
                user.Email, user.DisplayName,
                user.Roles.ToArray(), user.Permissions.ToArray()))
            : Results.Unauthorized();
}
