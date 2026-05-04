using FluentValidation;
using Giwu.Application.Common;
using Giwu.Contracts.Leaves;
using Giwu.Domain.Leaves;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Giwu.Application.Leaves.Commands;

public sealed record FileLeaveRequestCommand(FileLeaveRequest Request)
    : IRequest<Result<LeaveRequestDto>>;

public sealed class FileLeaveRequestValidator : AbstractValidator<FileLeaveRequestCommand>
{
    public FileLeaveRequestValidator()
    {
        RuleFor(x => x.Request.LeaveTypeId).NotEmpty();
        RuleFor(x => x.Request.StartDate).LessThanOrEqualTo(x => x.Request.EndDate)
            .WithMessage("Start date must be on or before end date.");
        RuleFor(x => x.Request.Reason).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Request.HalfDaySession)
            .Must(s => string.IsNullOrEmpty(s) || s == "AM" || s == "PM")
            .WithMessage("Half-day session must be AM, PM, or empty.");
    }
}

internal sealed class FileLeaveRequestHandler(
    IApplicationDbContext db,
    ICurrentUser user)
    : IRequestHandler<FileLeaveRequestCommand, Result<LeaveRequestDto>>
{
    public async Task<Result<LeaveRequestDto>> Handle(FileLeaveRequestCommand cmd, CancellationToken ct)
    {
        if (user.EmployeeId is null)
            return Result<LeaveRequestDto>.Forbidden("Only employees can file leave requests.");

        var r = cmd.Request;

        var emp = await db.Employees.FirstOrDefaultAsync(e => e.Id == user.EmployeeId, ct);
        if (emp is null) return Result<LeaveRequestDto>.NotFound("Employee profile not found");

        var type = await db.LeaveTypes.FirstOrDefaultAsync(t => t.Id == r.LeaveTypeId && t.IsActive, ct);
        if (type is null) return Result<LeaveRequestDto>.NotFound("Leave type not found");

        var days = WorkingDays(r.StartDate, r.EndDate, r.IsHalfDay);
        if (days <= 0)
            return Result<LeaveRequestDto>.NotFound("Date range yields zero working days.");

        var balance = await db.LeaveBalances.FirstOrDefaultAsync(b =>
            b.EmployeeId == emp.Id &&
            b.LeaveTypeId == type.Id &&
            b.PeriodYear == r.StartDate.Year, ct);

        if (balance is null)
        {
            balance = new LeaveBalance
            {
                EmployeeId  = emp.Id,
                LeaveTypeId = type.Id,
                PeriodYear  = r.StartDate.Year,
                Entitlement = type.AnnualEntitlementDays,
            };
            db.LeaveBalances.Add(balance);
        }

        if (balance.Available < days)
            return Result<LeaveRequestDto>.Forbidden(
                $"Insufficient {type.Code} balance. Available: {balance.Available}, requested: {days}.");

        var req = new LeaveRequest
        {
            EmployeeId      = emp.Id,
            LeaveTypeId     = type.Id,
            StartDate       = r.StartDate,
            EndDate         = r.EndDate,
            IsHalfDay       = r.IsHalfDay,
            HalfDaySession  = r.HalfDaySession,
            DaysRequested   = days,
            Reason          = r.Reason,
            Status          = LeaveRequestStatus.Pending,
        };
        db.LeaveRequests.Add(req);
        balance.Pending += days;

        await db.SaveChangesAsync(ct);

        return Result<LeaveRequestDto>.Success(new LeaveRequestDto(
            req.Id, emp.Id, emp.FullName,
            type.Id, type.Name, type.Code,
            req.StartDate, req.EndDate, req.IsHalfDay, req.DaysRequested,
            req.Reason, req.Status, req.CreatedAt));
    }

    private static decimal WorkingDays(DateOnly start, DateOnly end, bool halfDay)
    {
        if (end < start) return 0m;
        var days = 0;
        for (var d = start; d <= end; d = d.AddDays(1))
            if (d.DayOfWeek != DayOfWeek.Saturday && d.DayOfWeek != DayOfWeek.Sunday)
                days++;
        return halfDay && days >= 1 ? days - 0.5m : days;
    }
}
