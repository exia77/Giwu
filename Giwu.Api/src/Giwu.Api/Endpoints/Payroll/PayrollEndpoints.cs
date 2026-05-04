using FluentValidation;
using Giwu.Api.Common;
using Giwu.Api.Middleware;
using Giwu.Application.Common;
using Giwu.Application.Payroll.PayPeriods;
using Giwu.Application.Payroll.Payslips;
using Giwu.Contracts.Payroll;
using Giwu.Domain.Identity;
using Giwu.Domain.Payroll;
using MediatR;

namespace Giwu.Api.Endpoints.Payroll;

internal static class PayrollValidationProblem
{
    public static IResult ToProblem(ValidationException ex) =>
        Results.ValidationProblem(ex.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
}

// ── Pay Periods ─────────────────────────────────────────────────────────────
public sealed class ListPayPeriodsEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/payroll/periods", Handle)
           .RequireAuthorization(Permissions.Payroll.ViewAll)
           .WithTags("Payroll");

    private static async Task<IResult> Handle(
        IMediator m, int? year = null, PayPeriodStatus? status = null, CancellationToken ct = default) =>
        (await m.Send(new ListPayPeriodsQuery(year, status), ct)).ToHttp();
}

public sealed class GetPayPeriodEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/payroll/periods/{id:guid}", Handle)
           .RequireAuthorization(Permissions.Payroll.ViewAll)
           .WithTags("Payroll");

    private static async Task<IResult> Handle(Guid id, IMediator m, CancellationToken ct) =>
        (await m.Send(new GetPayPeriodQuery(id), ct)).ToHttp();
}

public sealed class CreatePayPeriodEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/api/payroll/periods", Handle)
           .RequireAuthorization(Permissions.Payroll.Run)
           .WithTags("Payroll");

    private static async Task<IResult> Handle(CreatePayPeriodRequest body, IMediator m, CancellationToken ct)
    {
        try
        {
            var r = await m.Send(new CreatePayPeriodCommand(body), ct);
            return r.Kind == ResultKind.Success
                ? Results.Created($"/api/payroll/periods/{r.Value!.Id}", r.Value)
                : r.ToHttp();
        }
        catch (ValidationException ex) { return PayrollValidationProblem.ToProblem(ex); }
    }
}

public sealed class UpdatePayPeriodEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPut("/api/payroll/periods/{id:guid}", Handle)
           .RequireAuthorization(Permissions.Payroll.Run)
           .WithTags("Payroll");

    private static async Task<IResult> Handle(Guid id, UpdatePayPeriodRequest body, IMediator m, CancellationToken ct) =>
        (await m.Send(new UpdatePayPeriodCommand(id, body), ct)).ToHttp();
}

public sealed class ApprovePayPeriodEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/api/payroll/periods/{id:guid}/approve", Handle)
           .RequireAuthorization(Permissions.Payroll.Approve)
           .WithTags("Payroll");

    private static async Task<IResult> Handle(Guid id, ApprovePayPeriodRequest body, IMediator m, CancellationToken ct) =>
        (await m.Send(new ApprovePayPeriodCommand(id, body.Note), ct)).ToHttp();
}

public sealed class ReleasePayPeriodEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/api/payroll/periods/{id:guid}/release", Handle)
           .RequireAuthorization(Permissions.Payroll.Approve)
           .WithTags("Payroll");

    private static async Task<IResult> Handle(Guid id, IMediator m, CancellationToken ct) =>
        (await m.Send(new ReleasePayPeriodCommand(id), ct)).ToHttp();
}

public sealed class DeletePayPeriodEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapDelete("/api/payroll/periods/{id:guid}", Handle)
           .RequireAuthorization(Permissions.Payroll.Run)
           .WithTags("Payroll");

    private static async Task<IResult> Handle(Guid id, IMediator m, CancellationToken ct) =>
        (await m.Send(new DeletePayPeriodCommand(id), ct)).ToHttp();
}

// ── Payslips ────────────────────────────────────────────────────────────────
public sealed class ListPayslipsEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/payroll/payslips", Handle)
           .RequireAuthorization(Permissions.Payroll.ViewAll)
           .WithTags("Payroll");

    private static async Task<IResult> Handle(
        IMediator m, Guid? payPeriodId = null, Guid? employeeId = null, CancellationToken ct = default) =>
        (await m.Send(new ListPayslipsQuery(payPeriodId, employeeId), ct)).ToHttp();
}

public sealed class GetPayslipEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/payroll/payslips/{id:guid}", Handle)
           .RequireAuthorization(Permissions.Payroll.ViewAll)
           .WithTags("Payroll");

    private static async Task<IResult> Handle(Guid id, IMediator m, CancellationToken ct) =>
        (await m.Send(new GetPayslipQuery(id), ct)).ToHttp();
}

public sealed class CreatePayslipEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/api/payroll/payslips", Handle)
           .RequireAuthorization(Permissions.Payroll.Run)
           .WithTags("Payroll");

    private static async Task<IResult> Handle(CreatePayslipRequest body, IMediator m, CancellationToken ct)
    {
        try
        {
            var r = await m.Send(new CreatePayslipCommand(body), ct);
            return r.Kind == ResultKind.Success
                ? Results.Created($"/api/payroll/payslips/{r.Value!.Id}", r.Value)
                : r.ToHttp();
        }
        catch (ValidationException ex) { return PayrollValidationProblem.ToProblem(ex); }
    }
}

public sealed class UpdatePayslipEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPut("/api/payroll/payslips/{id:guid}", Handle)
           .RequireAuthorization(Permissions.Payroll.Run)
           .WithTags("Payroll");

    private static async Task<IResult> Handle(Guid id, UpdatePayslipRequest body, IMediator m, CancellationToken ct) =>
        (await m.Send(new UpdatePayslipCommand(id, body), ct)).ToHttp();
}

public sealed class DeletePayslipEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapDelete("/api/payroll/payslips/{id:guid}", Handle)
           .RequireAuthorization(Permissions.Payroll.Run)
           .WithTags("Payroll");

    private static async Task<IResult> Handle(Guid id, IMediator m, CancellationToken ct) =>
        (await m.Send(new DeletePayslipCommand(id), ct)).ToHttp();
}
