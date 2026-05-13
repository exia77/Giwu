using FluentValidation;
using Giwu.Api.Common;
using Giwu.Api.Middleware;
using Giwu.Application.Attendance.Commands;
using Giwu.Application.Attendance.Overtime;
using Giwu.Application.Attendance.Queries;
using Giwu.Contracts.Attendance;
using Giwu.Domain.Attendance;
using Giwu.Domain.Identity;
using MediatR;

namespace Giwu.Api.Endpoints.Attendance;

public sealed class ClockInEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/api/attendance/clock-in", Handle)
           .RequireAuthorization(Permissions.Attendance.ViewSelf)
           .WithTags("Attendance");

    private static async Task<IResult> Handle(ClockInRequest body, IMediator m, CancellationToken ct) =>
        (await m.Send(new ClockInCommand(body), ct)).ToHttp();
}

public sealed class ClockOutEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/api/attendance/clock-out", Handle)
           .RequireAuthorization(Permissions.Attendance.ViewSelf)
           .WithTags("Attendance");

    private static async Task<IResult> Handle(ClockOutRequest body, IMediator m, CancellationToken ct) =>
        (await m.Send(new ClockOutCommand(body), ct)).ToHttp();
}

public sealed class ListAttendanceEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/attendance", Handle)
           .RequireAuthorization()
           .WithTags("Attendance");

    private static async Task<IResult> Handle(
        IMediator m,
        DateOnly? from = null, DateOnly? to = null, Guid? employeeId = null,
        int page = 1, int pageSize = 50, CancellationToken ct = default) =>
        (await m.Send(new ListAttendanceQuery(from, to, employeeId, page, pageSize), ct)).ToHttp();
}

public sealed class GetTodayAttendanceEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/attendance/today", Handle)
           .RequireAuthorization()
           .WithTags("Attendance");

    private static async Task<IResult> Handle(IMediator m, Guid? employeeId = null, CancellationToken ct = default) =>
        (await m.Send(new GetTodayAttendanceQuery(employeeId), ct)).ToHttp();
}

public sealed class UpdateAttendanceEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPut("/api/attendance/{id:guid}", Handle)
           .RequireAuthorization(Permissions.Attendance.Manage)
           .WithTags("Attendance");

    private static async Task<IResult> Handle(Guid id, UpdateAttendanceRequest body, IMediator m, CancellationToken ct) =>
        (await m.Send(new UpdateAttendanceCommand(id, body), ct)).ToHttp();
}

public sealed class ManualAttendanceEntryEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/api/attendance/manual", Handle)
           .RequireAuthorization(Permissions.Attendance.Manage)
           .WithTags("Attendance");

    private static async Task<IResult> Handle(ManualAttendanceEntryRequest body, IMediator m, CancellationToken ct) =>
        (await m.Send(new ManualAttendanceEntryCommand(body), ct)).ToHttp();
}

// ── Overtime ────────────────────────────────────────────────────────────────
public sealed class ListOvertimeRequestsEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/overtime-requests", Handle)
           .RequireAuthorization()
           .WithTags("Attendance");

    private static async Task<IResult> Handle(
        IMediator m, ApprovalStatus? status = null, Guid? employeeId = null, bool mineOnly = false,
        CancellationToken ct = default) =>
        (await m.Send(new ListOvertimeRequestsQuery(status, employeeId, mineOnly), ct)).ToHttp();
}

public sealed class FileOvertimeEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/api/overtime-requests", Handle)
           .RequireAuthorization()
           .WithTags("Attendance");

    private static async Task<IResult> Handle(FileOvertimeRequest body, IMediator m, CancellationToken ct)
    {
        try { return (await m.Send(new FileOvertimeCommand(body), ct)).ToHttp(); }
        catch (ValidationException ex)
        {
            return Results.ValidationProblem(ex.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
        }
    }
}

public sealed class ApproveOvertimeEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/api/overtime-requests/{id:guid}/approve", Handle)
           .RequireAuthorization(Permissions.Attendance.Manage)
           .WithTags("Attendance");

    private static async Task<IResult> Handle(Guid id, ResolveOvertimeRequest body, IMediator m, CancellationToken ct) =>
        (await m.Send(new ResolveOvertimeCommand(id, true, body.Note), ct)).ToHttp();
}

public sealed class RejectOvertimeEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/api/overtime-requests/{id:guid}/reject", Handle)
           .RequireAuthorization(Permissions.Attendance.Manage)
           .WithTags("Attendance");

    private static async Task<IResult> Handle(Guid id, ResolveOvertimeRequest body, IMediator m, CancellationToken ct) =>
        (await m.Send(new ResolveOvertimeCommand(id, false, body.Note), ct)).ToHttp();
}
