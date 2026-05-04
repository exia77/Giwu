using Giwu.Api.Common;

namespace Giwu.Api.Endpoints.Health;

public sealed class HealthEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapHealthChecks("/health/live")
           .AllowAnonymous()
           .WithTags("Health");

        app.MapHealthChecks("/health/ready")
           .AllowAnonymous()
           .WithTags("Health");
    }
}
