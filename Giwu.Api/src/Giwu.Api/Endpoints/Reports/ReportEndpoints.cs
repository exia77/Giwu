using Giwu.Api.Common;
using Giwu.Api.Middleware;
using Giwu.Application.Reports;
using Giwu.Domain.Identity;
using MediatR;

namespace Giwu.Api.Endpoints.Reports;

public sealed class HeadcountSummaryEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/reports/headcount", Handle)
           .RequireAuthorization(Permissions.Reports.View)
           .WithTags("Reports");

    private static async Task<IResult> Handle(IMediator m, CancellationToken ct) =>
        (await m.Send(new HeadcountSummaryQuery(), ct)).ToHttp();
}

public sealed class AttendanceSummaryEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/reports/attendance", Handle)
           .RequireAuthorization(Permissions.Reports.View)
           .WithTags("Reports");

    private static async Task<IResult> Handle(
        IMediator m, DateOnly? from = null, DateOnly? to = null, CancellationToken ct = default)
    {
        var f = from ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));
        var t = to   ?? DateOnly.FromDateTime(DateTime.UtcNow);
        return (await m.Send(new AttendanceSummaryQuery(f, t), ct)).ToHttp();
    }
}

public sealed class LeaveSummaryEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/reports/leave", Handle)
           .RequireAuthorization(Permissions.Reports.View)
           .WithTags("Reports");

    private static async Task<IResult> Handle(IMediator m, CancellationToken ct) =>
        (await m.Send(new LeaveSummaryQuery(), ct)).ToHttp();
}

public sealed class PayrollSummaryEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/reports/payroll", Handle)
           .RequireAuthorization(Permissions.Reports.View)
           .WithTags("Reports");

    private static async Task<IResult> Handle(IMediator m, int? year = null, CancellationToken ct = default) =>
        (await m.Send(new PayrollSummaryQuery(year), ct)).ToHttp();
}
