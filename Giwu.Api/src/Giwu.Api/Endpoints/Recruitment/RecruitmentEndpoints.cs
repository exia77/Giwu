using FluentValidation;
using Giwu.Api.Common;
using Giwu.Api.Middleware;
using Giwu.Application.Common;
using Giwu.Application.Recruitment.Candidates;
using Giwu.Application.Recruitment.Interviews;
using Giwu.Application.Recruitment.Jobs;
using Giwu.Contracts.Recruitment;
using Giwu.Domain.Identity;
using Giwu.Domain.Recruitment;
using MediatR;

namespace Giwu.Api.Endpoints.Recruitment;

internal static class RecruitmentValidationProblem
{
    public static IResult ToProblem(ValidationException ex) =>
        Results.ValidationProblem(ex.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
}

// ── Jobs ────────────────────────────────────────────────────────────────────
public sealed class ListJobsEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/recruitment/jobs", Handle)
           .RequireAuthorization(Permissions.Recruitment.View)
           .WithTags("Recruitment");

    private static async Task<IResult> Handle(
        IMediator m, JobStatus? status = null, Guid? departmentId = null, string? search = null,
        CancellationToken ct = default) =>
        (await m.Send(new ListJobsQuery(status, departmentId, search), ct)).ToHttp();
}

public sealed class GetJobEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/recruitment/jobs/{id:guid}", Handle)
           .RequireAuthorization(Permissions.Recruitment.View)
           .WithTags("Recruitment");

    private static async Task<IResult> Handle(Guid id, IMediator m, CancellationToken ct) =>
        (await m.Send(new GetJobQuery(id), ct)).ToHttp();
}

public sealed class CreateJobEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/api/recruitment/jobs", Handle)
           .RequireAuthorization(Permissions.Recruitment.Manage)
           .WithTags("Recruitment");

    private static async Task<IResult> Handle(CreateJobRequisitionRequest body, IMediator m, CancellationToken ct)
    {
        try
        {
            var r = await m.Send(new CreateJobCommand(body), ct);
            return r.Kind == ResultKind.Success
                ? Results.Created($"/api/recruitment/jobs/{r.Value!.Id}", r.Value)
                : r.ToHttp();
        }
        catch (ValidationException ex) { return RecruitmentValidationProblem.ToProblem(ex); }
    }
}

public sealed class UpdateJobEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPut("/api/recruitment/jobs/{id:guid}", Handle)
           .RequireAuthorization(Permissions.Recruitment.Manage)
           .WithTags("Recruitment");

    private static async Task<IResult> Handle(Guid id, UpdateJobRequisitionRequest body, IMediator m, CancellationToken ct)
    {
        try { return (await m.Send(new UpdateJobCommand(id, body), ct)).ToHttp(); }
        catch (ValidationException ex) { return RecruitmentValidationProblem.ToProblem(ex); }
    }
}

public sealed class ChangeJobStatusEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/api/recruitment/jobs/{id:guid}/status", Handle)
           .RequireAuthorization(Permissions.Recruitment.Manage)
           .WithTags("Recruitment");

    private static async Task<IResult> Handle(Guid id, ChangeJobStatusRequest body, IMediator m, CancellationToken ct) =>
        (await m.Send(new ChangeJobStatusCommand(id, body.Status), ct)).ToHttp();
}

public sealed class DeleteJobEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapDelete("/api/recruitment/jobs/{id:guid}", Handle)
           .RequireAuthorization(Permissions.Recruitment.Manage)
           .WithTags("Recruitment");

    private static async Task<IResult> Handle(Guid id, IMediator m, CancellationToken ct) =>
        (await m.Send(new DeleteJobCommand(id), ct)).ToHttp();
}

// ── Candidates ──────────────────────────────────────────────────────────────
public sealed class ListCandidatesEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/recruitment/candidates", Handle)
           .RequireAuthorization(Permissions.Recruitment.View)
           .WithTags("Recruitment");

    private static async Task<IResult> Handle(
        IMediator m, CandidateStage? stage = null, Guid? jobId = null, string? source = null,
        int? minRating = null, string? search = null, CancellationToken ct = default) =>
        (await m.Send(new ListCandidatesQuery(stage, jobId, source, minRating, search), ct)).ToHttp();
}

public sealed class GetCandidateEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/recruitment/candidates/{id:guid}", Handle)
           .RequireAuthorization(Permissions.Recruitment.View)
           .WithTags("Recruitment");

    private static async Task<IResult> Handle(Guid id, IMediator m, CancellationToken ct) =>
        (await m.Send(new GetCandidateQuery(id), ct)).ToHttp();
}

public sealed class CreateCandidateEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/api/recruitment/candidates", Handle)
           .RequireAuthorization(Permissions.Recruitment.Manage)
           .WithTags("Recruitment");

    private static async Task<IResult> Handle(CreateCandidateRequest body, IMediator m, CancellationToken ct)
    {
        try
        {
            var r = await m.Send(new CreateCandidateCommand(body), ct);
            return r.Kind == ResultKind.Success
                ? Results.Created($"/api/recruitment/candidates/{r.Value!.Id}", r.Value)
                : r.ToHttp();
        }
        catch (ValidationException ex) { return RecruitmentValidationProblem.ToProblem(ex); }
    }
}

public sealed class UpdateCandidateEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPut("/api/recruitment/candidates/{id:guid}", Handle)
           .RequireAuthorization(Permissions.Recruitment.Manage)
           .WithTags("Recruitment");

    private static async Task<IResult> Handle(Guid id, UpdateCandidateRequest body, IMediator m, CancellationToken ct)
    {
        try { return (await m.Send(new UpdateCandidateCommand(id, body), ct)).ToHttp(); }
        catch (ValidationException ex) { return RecruitmentValidationProblem.ToProblem(ex); }
    }
}

public sealed class AdvanceCandidateEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/api/recruitment/candidates/{id:guid}/advance", Handle)
           .RequireAuthorization(Permissions.Recruitment.Manage)
           .WithTags("Recruitment");

    private static async Task<IResult> Handle(Guid id, AdvanceCandidateRequest body, IMediator m, CancellationToken ct) =>
        (await m.Send(new AdvanceCandidateCommand(id, body.NewStage, body.Note), ct)).ToHttp();
}

public sealed class RejectCandidateEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/api/recruitment/candidates/{id:guid}/reject", Handle)
           .RequireAuthorization(Permissions.Recruitment.Manage)
           .WithTags("Recruitment");

    private static async Task<IResult> Handle(Guid id, RejectCandidateRequest body, IMediator m, CancellationToken ct) =>
        (await m.Send(new RejectCandidateCommand(id, body.Reason), ct)).ToHttp();
}

public sealed class DeleteCandidateEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapDelete("/api/recruitment/candidates/{id:guid}", Handle)
           .RequireAuthorization(Permissions.Recruitment.Manage)
           .WithTags("Recruitment");

    private static async Task<IResult> Handle(Guid id, IMediator m, CancellationToken ct) =>
        (await m.Send(new DeleteCandidateCommand(id), ct)).ToHttp();
}

// ── Interviews ──────────────────────────────────────────────────────────────
public sealed class ListInterviewsEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/recruitment/interviews", Handle)
           .RequireAuthorization(Permissions.Recruitment.View)
           .WithTags("Recruitment");

    private static async Task<IResult> Handle(
        IMediator m, Guid? candidateId = null, DateTimeOffset? from = null, DateTimeOffset? to = null,
        InterviewStatus? status = null, CancellationToken ct = default) =>
        (await m.Send(new ListInterviewsQuery(candidateId, from, to, status), ct)).ToHttp();
}

public sealed class ScheduleInterviewEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/api/recruitment/interviews", Handle)
           .RequireAuthorization(Permissions.Recruitment.Manage)
           .WithTags("Recruitment");

    private static async Task<IResult> Handle(ScheduleInterviewRequest body, IMediator m, CancellationToken ct)
    {
        try
        {
            var r = await m.Send(new ScheduleInterviewCommand(body), ct);
            return r.Kind == ResultKind.Success
                ? Results.Created($"/api/recruitment/interviews/{r.Value!.Id}", r.Value)
                : r.ToHttp();
        }
        catch (ValidationException ex) { return RecruitmentValidationProblem.ToProblem(ex); }
    }
}

public sealed class UpdateInterviewEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPut("/api/recruitment/interviews/{id:guid}", Handle)
           .RequireAuthorization(Permissions.Recruitment.Manage)
           .WithTags("Recruitment");

    private static async Task<IResult> Handle(Guid id, UpdateInterviewRequest body, IMediator m, CancellationToken ct) =>
        (await m.Send(new UpdateInterviewCommand(id, body), ct)).ToHttp();
}

public sealed class CancelInterviewEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/api/recruitment/interviews/{id:guid}/cancel", Handle)
           .RequireAuthorization(Permissions.Recruitment.Manage)
           .WithTags("Recruitment");

    private static async Task<IResult> Handle(Guid id, IMediator m, CancellationToken ct) =>
        (await m.Send(new CancelInterviewCommand(id), ct)).ToHttp();
}
