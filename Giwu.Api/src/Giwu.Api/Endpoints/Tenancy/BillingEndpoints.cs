using System.IO;
using Giwu.Api.Common;
using Giwu.Api.Middleware;
using Giwu.Application.Common;
using Giwu.Application.Subscriptions;
using Giwu.Contracts.Tenancy;
using Giwu.Domain.Identity;
using MediatR;

namespace Giwu.Api.Endpoints.Tenancy;

public sealed class CreateCheckoutSessionEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/api/billing/checkout", Handle)
           .RequireAuthorization(Permissions.Settings.Manage)
           .WithTags("Billing")
           .WithSummary("Creates a Stripe Checkout Session for the target tier and returns the hosted URL");

    private static async Task<IResult> Handle(
        CreateCheckoutSessionRequest body, IMediator m, CancellationToken ct) =>
        (await m.Send(new CreateCheckoutSessionCommand(body.Tier), ct)).ToHttp();
}

/// <summary>
/// Stripe webhook receiver. Unauthenticated (Stripe signs the payload with the
/// webhook secret instead of using our JWT). Reads the raw request body — must
/// not be deserialized to a DTO because that would mangle the signature input.
/// </summary>
public sealed class StripeWebhookEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/api/billing/webhook", Handle)
           .AllowAnonymous()
           .WithTags("Billing")
           .WithSummary("Stripe webhook receiver — signature-verified, updates tenant subscription state");

    private static async Task<IResult> Handle(
        HttpRequest req,
        IBillingService billing,
        IMediator m,
        CancellationToken ct)
    {
        using var reader = new StreamReader(req.Body);
        var payload = await reader.ReadToEndAsync(ct);
        var sig = req.Headers["Stripe-Signature"].ToString();

        var evt = billing.VerifyWebhook(payload, sig);
        if (evt is null) return Results.BadRequest(new { message = "Invalid webhook signature." });

        var result = await m.Send(new HandleBillingWebhookCommand(evt), ct);
        // Always 200 unless the handler genuinely failed, so Stripe doesn't keep
        // retrying on no-op events we chose to ignore.
        return result.Kind == ResultKind.Success
            ? Results.Ok(new { received = true })
            : result.ToHttp();
    }
}
