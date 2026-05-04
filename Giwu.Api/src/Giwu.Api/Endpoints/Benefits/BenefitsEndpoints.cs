using FluentValidation;
using Giwu.Api.Common;
using Giwu.Api.Middleware;
using Giwu.Application.Benefits.Enrollments;
using Giwu.Application.Benefits.Programs;
using Giwu.Application.Common;
using Giwu.Contracts.Benefits;
using Giwu.Domain.Benefits;
using Giwu.Domain.Identity;
using MediatR;

namespace Giwu.Api.Endpoints.Benefits;

internal static class BenefitsValidationProblem
{
    public static IResult ToProblem(ValidationException ex) =>
        Results.ValidationProblem(ex.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
}

// ── Programs ────────────────────────────────────────────────────────────────
public sealed class ListBenefitProgramsEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/benefits/programs", Handle)
           .RequireAuthorization(Permissions.Benefits.View)
           .WithTags("Benefits");

    private static async Task<IResult> Handle(
        IMediator m, bool includeInactive = false, BenefitCategory? category = null, CancellationToken ct = default) =>
        (await m.Send(new ListBenefitProgramsQuery(includeInactive, category), ct)).ToHttp();
}

public sealed class GetBenefitProgramEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/benefits/programs/{id:guid}", Handle)
           .RequireAuthorization(Permissions.Benefits.View)
           .WithTags("Benefits");

    private static async Task<IResult> Handle(Guid id, IMediator m, CancellationToken ct) =>
        (await m.Send(new GetBenefitProgramQuery(id), ct)).ToHttp();
}

public sealed class CreateBenefitProgramEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/api/benefits/programs", Handle)
           .RequireAuthorization(Permissions.Benefits.Manage)
           .WithTags("Benefits");

    private static async Task<IResult> Handle(CreateBenefitProgramRequest body, IMediator m, CancellationToken ct)
    {
        try
        {
            var r = await m.Send(new CreateBenefitProgramCommand(body), ct);
            return r.Kind == ResultKind.Success
                ? Results.Created($"/api/benefits/programs/{r.Value!.Id}", r.Value)
                : r.ToHttp();
        }
        catch (ValidationException ex) { return BenefitsValidationProblem.ToProblem(ex); }
    }
}

public sealed class UpdateBenefitProgramEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPut("/api/benefits/programs/{id:guid}", Handle)
           .RequireAuthorization(Permissions.Benefits.Manage)
           .WithTags("Benefits");

    private static async Task<IResult> Handle(Guid id, UpdateBenefitProgramRequest body, IMediator m, CancellationToken ct) =>
        (await m.Send(new UpdateBenefitProgramCommand(id, body), ct)).ToHttp();
}

public sealed class DeleteBenefitProgramEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapDelete("/api/benefits/programs/{id:guid}", Handle)
           .RequireAuthorization(Permissions.Benefits.Manage)
           .WithTags("Benefits");

    private static async Task<IResult> Handle(Guid id, IMediator m, CancellationToken ct) =>
        (await m.Send(new DeleteBenefitProgramCommand(id), ct)).ToHttp();
}

// ── Enrollments ─────────────────────────────────────────────────────────────
public sealed class ListEnrollmentsEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/benefits/enrollments", Handle)
           .RequireAuthorization(Permissions.Benefits.View)
           .WithTags("Benefits");

    private static async Task<IResult> Handle(
        IMediator m, Guid? employeeId = null, Guid? programId = null,
        EnrollmentStatus? status = null, CancellationToken ct = default) =>
        (await m.Send(new ListEnrollmentsQuery(employeeId, programId, status), ct)).ToHttp();
}

public sealed class CreateEnrollmentEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/api/benefits/enrollments", Handle)
           .RequireAuthorization(Permissions.Benefits.Manage)
           .WithTags("Benefits");

    private static async Task<IResult> Handle(CreateBenefitEnrollmentRequest body, IMediator m, CancellationToken ct)
    {
        try
        {
            var r = await m.Send(new CreateEnrollmentCommand(body), ct);
            return r.Kind == ResultKind.Success
                ? Results.Created($"/api/benefits/enrollments/{r.Value!.Id}", r.Value)
                : r.ToHttp();
        }
        catch (ValidationException ex) { return BenefitsValidationProblem.ToProblem(ex); }
    }
}

public sealed class UpdateEnrollmentEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPut("/api/benefits/enrollments/{id:guid}", Handle)
           .RequireAuthorization(Permissions.Benefits.Manage)
           .WithTags("Benefits");

    private static async Task<IResult> Handle(Guid id, UpdateBenefitEnrollmentRequest body, IMediator m, CancellationToken ct) =>
        (await m.Send(new UpdateEnrollmentCommand(id, body), ct)).ToHttp();
}

public sealed class DeleteEnrollmentEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapDelete("/api/benefits/enrollments/{id:guid}", Handle)
           .RequireAuthorization(Permissions.Benefits.Manage)
           .WithTags("Benefits");

    private static async Task<IResult> Handle(Guid id, IMediator m, CancellationToken ct) =>
        (await m.Send(new DeleteEnrollmentCommand(id), ct)).ToHttp();
}

// ── Benefit Requests ────────────────────────────────────────────────────────
public sealed class ListBenefitRequestsEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/benefits/requests", Handle)
           .RequireAuthorization(Permissions.Benefits.View)
           .WithTags("Benefits");

    private static async Task<IResult> Handle(
        IMediator m, Guid? employeeId = null, BenefitRequestStatus? status = null,
        BenefitRequestType? type = null, CancellationToken ct = default) =>
        (await m.Send(new ListBenefitRequestsQuery(employeeId, status, type), ct)).ToHttp();
}

public sealed class FileBenefitRequestEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/api/benefits/requests", Handle)
           .RequireAuthorization()
           .WithTags("Benefits");

    private static async Task<IResult> Handle(FileBenefitRequestRequest body, IMediator m, CancellationToken ct)
    {
        try
        {
            var r = await m.Send(new FileBenefitRequestCommand(body), ct);
            return r.Kind == ResultKind.Success
                ? Results.Created($"/api/benefits/requests/{r.Value!.Id}", r.Value)
                : r.ToHttp();
        }
        catch (ValidationException ex) { return BenefitsValidationProblem.ToProblem(ex); }
    }
}

public sealed class ResolveBenefitRequestEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/api/benefits/requests/{id:guid}/resolve", Handle)
           .RequireAuthorization(Permissions.Benefits.Manage)
           .WithTags("Benefits");

    private static async Task<IResult> Handle(Guid id, ResolveBenefitRequestRequest body, IMediator m, CancellationToken ct) =>
        (await m.Send(new ResolveBenefitRequestCommand(id, body.Status, body.Note), ct)).ToHttp();
}
