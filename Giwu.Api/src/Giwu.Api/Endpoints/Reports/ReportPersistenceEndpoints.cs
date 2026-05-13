using FluentValidation;
using Giwu.Api.Common;
using Giwu.Api.Middleware;
using Giwu.Application.Reports.Persisted;
using Giwu.Contracts.Reports;
using Giwu.Domain.Identity;
using MediatR;

namespace Giwu.Api.Endpoints.Reports;

// ────────── Report Definitions ──────────

public sealed class ListReportDefinitionsEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/reports/definitions", Handle)
           .RequireAuthorization(Permissions.Reports.View)
           .WithTags("Reports");

    private static async Task<IResult> Handle(IMediator m, CancellationToken ct) =>
        (await m.Send(new ListReportDefinitionsQuery(), ct)).ToHttp();
}

public sealed class CreateReportDefinitionEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/api/reports/definitions", Handle)
           .RequireAuthorization(Permissions.Reports.Run)
           .WithTags("Reports");

    private static async Task<IResult> Handle(
        CreateReportDefinitionRequest body, IMediator m, CancellationToken ct)
    {
        try
        {
            return (await m.Send(new CreateReportDefinitionCommand(body), ct)).ToHttp();
        }
        catch (ValidationException ex) { return _Validation.ToValidationResult(ex); }
    }
}

public sealed class UpdateReportDefinitionEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPut("/api/reports/definitions/{id:guid}", Handle)
           .RequireAuthorization(Permissions.Reports.Run)
           .WithTags("Reports");

    private static async Task<IResult> Handle(
        Guid id, UpdateReportDefinitionRequest body, IMediator m, CancellationToken ct) =>
        (await m.Send(new UpdateReportDefinitionCommand(id, body), ct)).ToHttp();
}

public sealed class DeleteReportDefinitionEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapDelete("/api/reports/definitions/{id:guid}", Handle)
           .RequireAuthorization(Permissions.Reports.Run)
           .WithTags("Reports");

    private static async Task<IResult> Handle(Guid id, IMediator m, CancellationToken ct) =>
        (await m.Send(new DeleteReportDefinitionCommand(id), ct)).ToHttp();
}

// ────────── Report Schedules ──────────

public sealed class ListReportSchedulesEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/reports/schedules", Handle)
           .RequireAuthorization(Permissions.Reports.View)
           .WithTags("Reports");

    private static async Task<IResult> Handle(IMediator m, CancellationToken ct) =>
        (await m.Send(new ListReportSchedulesQuery(), ct)).ToHttp();
}

public sealed class CreateReportScheduleEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/api/reports/schedules", Handle)
           .RequireAuthorization(Permissions.Reports.Run)
           .WithTags("Reports");

    private static async Task<IResult> Handle(
        CreateReportScheduleRequest body, IMediator m, CancellationToken ct)
    {
        try
        {
            return (await m.Send(new CreateReportScheduleCommand(body), ct)).ToHttp();
        }
        catch (ValidationException ex) { return _Validation.ToValidationResult(ex); }
    }
}

public sealed class UpdateReportScheduleEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPut("/api/reports/schedules/{id:guid}", Handle)
           .RequireAuthorization(Permissions.Reports.Run)
           .WithTags("Reports");

    private static async Task<IResult> Handle(
        Guid id, UpdateReportScheduleRequest body, IMediator m, CancellationToken ct) =>
        (await m.Send(new UpdateReportScheduleCommand(id, body), ct)).ToHttp();
}

public sealed class ToggleReportScheduleEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/api/reports/schedules/{id:guid}/toggle", Handle)
           .RequireAuthorization(Permissions.Reports.Run)
           .WithTags("Reports");

    private static async Task<IResult> Handle(Guid id, IMediator m, CancellationToken ct) =>
        (await m.Send(new ToggleReportScheduleCommand(id), ct)).ToHttp();
}

public sealed class DeleteReportScheduleEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapDelete("/api/reports/schedules/{id:guid}", Handle)
           .RequireAuthorization(Permissions.Reports.Run)
           .WithTags("Reports");

    private static async Task<IResult> Handle(Guid id, IMediator m, CancellationToken ct) =>
        (await m.Send(new DeleteReportScheduleCommand(id), ct)).ToHttp();
}

// ────────── Report Runs ──────────

public sealed class ListReportRunsEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/reports/runs", Handle)
           .RequireAuthorization(Permissions.Reports.View)
           .WithTags("Reports");

    private static async Task<IResult> Handle(IMediator m, int limit = 200, CancellationToken ct = default) =>
        (await m.Send(new ListReportRunsQuery(limit), ct)).ToHttp();
}

public sealed class QueueReportRunEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/api/reports/runs", Handle)
           .RequireAuthorization(Permissions.Reports.Run)
           .WithTags("Reports");

    private static async Task<IResult> Handle(
        QueueReportRunRequest body, IMediator m, CancellationToken ct)
    {
        try
        {
            return (await m.Send(new QueueReportRunCommand(body), ct)).ToHttp();
        }
        catch (ValidationException ex) { return _Validation.ToValidationResult(ex); }
    }
}

// ────────── Compliance Deadlines ──────────

public sealed class ListComplianceDeadlinesEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/reports/compliance", Handle)
           .RequireAuthorization(Permissions.Reports.View)
           .WithTags("Reports");

    private static async Task<IResult> Handle(IMediator m, CancellationToken ct) =>
        (await m.Send(new ListComplianceDeadlinesQuery(), ct)).ToHttp();
}

public sealed class CreateComplianceDeadlineEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/api/reports/compliance", Handle)
           .RequireAuthorization(Permissions.Reports.Run)
           .WithTags("Reports");

    private static async Task<IResult> Handle(
        CreateComplianceDeadlineRequest body, IMediator m, CancellationToken ct)
    {
        try
        {
            return (await m.Send(new CreateComplianceDeadlineCommand(body), ct)).ToHttp();
        }
        catch (ValidationException ex) { return _Validation.ToValidationResult(ex); }
    }
}

public sealed class UpdateComplianceDeadlineEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPut("/api/reports/compliance/{id:guid}", Handle)
           .RequireAuthorization(Permissions.Reports.Run)
           .WithTags("Reports");

    private static async Task<IResult> Handle(
        Guid id, UpdateComplianceDeadlineRequest body, IMediator m, CancellationToken ct) =>
        (await m.Send(new UpdateComplianceDeadlineCommand(id, body), ct)).ToHttp();
}

public sealed class SetComplianceFiledEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/api/reports/compliance/{id:guid}/filed", Handle)
           .RequireAuthorization(Permissions.Reports.Run)
           .WithTags("Reports");

    private static async Task<IResult> Handle(
        Guid id, SetFiledBody body, IMediator m, CancellationToken ct) =>
        (await m.Send(new SetComplianceFiledCommand(id, body.Filed), ct)).ToHttp();
}

public sealed record SetFiledBody(bool Filed);

internal static class _Validation
{
    public static IResult ToValidationResult(ValidationException ex)
    {
        var errors = ex.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
        return Results.ValidationProblem(errors);
    }
}

