using Giwu.Api.Common;
using Giwu.Api.Middleware;
using Giwu.Application.Subscriptions;
using Giwu.Contracts.Tenancy;
using Giwu.Domain.Identity;
using MediatR;

namespace Giwu.Api.Endpoints.Tenancy;

public sealed class GetSubscriptionEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/tenants/me/subscription", Handle)
           .RequireAuthorization()
           .WithTags("Tenancy")
           .WithSummary("Returns the current tenant's subscription tier, status, and usage vs limits");

    private static async Task<IResult> Handle(IMediator m, CancellationToken ct) =>
        (await m.Send(new GetSubscriptionQuery(), ct)).ToHttp();
}

public sealed class ChangeSubscriptionTierEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/api/tenants/me/subscription", Handle)
           .RequireAuthorization(Permissions.Settings.Manage)
           .WithTags("Tenancy")
           .WithSummary("Manually switch the subscription tier (demo). Replaced by Stripe webhook in production");

    private static async Task<IResult> Handle(
        ChangeSubscriptionTierRequest body, IMediator m, CancellationToken ct) =>
        (await m.Send(new ChangeSubscriptionTierCommand(body.Tier), ct)).ToHttp();
}
