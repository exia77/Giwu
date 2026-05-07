using FluentValidation;
using Giwu.Api.Common;
using Giwu.Api.Middleware;
using Giwu.Application.Settings;
using Giwu.Contracts.Settings;
using Giwu.Domain.Identity;
using MediatR;

namespace Giwu.Api.Endpoints.Settings;

// ── Branding ────────────────────────────────────────────────────────────────
public sealed class GetBrandingSettingsEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/settings/branding", Handle)
           .RequireAuthorization(Permissions.Settings.View)
           .WithTags("Settings");

    private static async Task<IResult> Handle(IMediator m, CancellationToken ct) =>
        (await m.Send(new GetBrandingSettingsQuery(), ct)).ToHttp();
}

public sealed class UpdateBrandingSettingsEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPut("/api/settings/branding", Handle)
           .RequireAuthorization(Permissions.Settings.Manage)
           .WithTags("Settings");

    private static async Task<IResult> Handle(UpdateBrandingSettingsRequest body, IMediator m, CancellationToken ct)
    {
        try { return (await m.Send(new UpdateBrandingSettingsCommand(body), ct)).ToHttp(); }
        catch (ValidationException ex)
        {
            return Results.ValidationProblem(ex.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
        }
    }
}

// ── Reset to Defaults ───────────────────────────────────────────────────────
public sealed class ResetSettingsEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/api/settings/reset", Handle)
           .RequireAuthorization(Permissions.Settings.Manage)
           .WithTags("Settings")
           .WithSummary("Resets Branding/Localization/Payroll/Notifications/Security to factory defaults. Does not touch Organization registration numbers.");

    private static async Task<IResult> Handle(IMediator m, CancellationToken ct) =>
        (await m.Send(new ResetSettingsCommand(), ct)).ToHttp();
}

// ── Organization ────────────────────────────────────────────────────────────
public sealed class GetOrganizationProfileEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/settings/organization", Handle)
           .RequireAuthorization(Permissions.Settings.View)
           .WithTags("Settings");

    private static async Task<IResult> Handle(IMediator m, CancellationToken ct) =>
        (await m.Send(new GetOrganizationProfileQuery(), ct)).ToHttp();
}

public sealed class UpdateOrganizationProfileEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPut("/api/settings/organization", Handle)
           .RequireAuthorization(Permissions.Settings.Manage)
           .WithTags("Settings");

    private static async Task<IResult> Handle(UpdateOrganizationProfileRequest body, IMediator m, CancellationToken ct)
    {
        try { return (await m.Send(new UpdateOrganizationProfileCommand(body), ct)).ToHttp(); }
        catch (ValidationException ex)
        {
            return Results.ValidationProblem(ex.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
        }
    }
}

// ── Localization ────────────────────────────────────────────────────────────
public sealed class GetLocalizationSettingsEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/settings/localization", Handle)
           .RequireAuthorization(Permissions.Settings.View)
           .WithTags("Settings");

    private static async Task<IResult> Handle(IMediator m, CancellationToken ct) =>
        (await m.Send(new GetLocalizationSettingsQuery(), ct)).ToHttp();
}

public sealed class UpdateLocalizationSettingsEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPut("/api/settings/localization", Handle)
           .RequireAuthorization(Permissions.Settings.Manage)
           .WithTags("Settings");

    private static async Task<IResult> Handle(UpdateLocalizationSettingsRequest body, IMediator m, CancellationToken ct)
    {
        try { return (await m.Send(new UpdateLocalizationSettingsCommand(body), ct)).ToHttp(); }
        catch (ValidationException ex)
        {
            return Results.ValidationProblem(ex.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
        }
    }
}

// ── Payroll defaults ────────────────────────────────────────────────────────
public sealed class GetPayrollDefaultsEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/settings/payroll", Handle)
           .RequireAuthorization(Permissions.Settings.View)
           .WithTags("Settings");

    private static async Task<IResult> Handle(IMediator m, CancellationToken ct) =>
        (await m.Send(new GetPayrollDefaultsQuery(), ct)).ToHttp();
}

public sealed class UpdatePayrollDefaultsEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPut("/api/settings/payroll", Handle)
           .RequireAuthorization(Permissions.Settings.Manage)
           .WithTags("Settings");

    private static async Task<IResult> Handle(UpdatePayrollDefaultsRequest body, IMediator m, CancellationToken ct)
    {
        try { return (await m.Send(new UpdatePayrollDefaultsCommand(body), ct)).ToHttp(); }
        catch (ValidationException ex)
        {
            return Results.ValidationProblem(ex.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
        }
    }
}

// ── Notifications ───────────────────────────────────────────────────────────
public sealed class GetNotificationSettingsEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/settings/notifications", Handle)
           .RequireAuthorization(Permissions.Settings.View)
           .WithTags("Settings");

    private static async Task<IResult> Handle(IMediator m, CancellationToken ct) =>
        (await m.Send(new GetNotificationSettingsQuery(), ct)).ToHttp();
}

public sealed class UpdateNotificationSettingsEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPut("/api/settings/notifications", Handle)
           .RequireAuthorization(Permissions.Settings.Manage)
           .WithTags("Settings");

    private static async Task<IResult> Handle(UpdateNotificationSettingsRequest body, IMediator m, CancellationToken ct)
    {
        try { return (await m.Send(new UpdateNotificationSettingsCommand(body), ct)).ToHttp(); }
        catch (ValidationException ex)
        {
            return Results.ValidationProblem(ex.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
        }
    }
}

// ── Security ────────────────────────────────────────────────────────────────
public sealed class GetSecuritySettingsEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/settings/security", Handle)
           .RequireAuthorization(Permissions.Settings.View)
           .WithTags("Settings");

    private static async Task<IResult> Handle(IMediator m, CancellationToken ct) =>
        (await m.Send(new GetSecuritySettingsQuery(), ct)).ToHttp();
}

public sealed class UpdateSecuritySettingsEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPut("/api/settings/security", Handle)
           .RequireAuthorization(Permissions.Settings.Manage)
           .WithTags("Settings");

    private static async Task<IResult> Handle(UpdateSecuritySettingsRequest body, IMediator m, CancellationToken ct)
    {
        try { return (await m.Send(new UpdateSecuritySettingsCommand(body), ct)).ToHttp(); }
        catch (ValidationException ex)
        {
            return Results.ValidationProblem(ex.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
        }
    }
}
