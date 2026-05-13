using FluentValidation;
using Giwu.Application.Common;
using Giwu.Application.Notifications;
using Giwu.Domain.Leaves;
using Giwu.Domain.Notifications;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Giwu.Application.Leaves.Commands;

public sealed record ApproveLeaveRequestCommand(Guid RequestId, string Note) : IRequest<Result>;

public sealed class ApproveLeaveRequestValidator : AbstractValidator<ApproveLeaveRequestCommand>
{
    public ApproveLeaveRequestValidator()
    {
        RuleFor(x => x.RequestId).NotEmpty();
        RuleFor(x => x.Note).MaximumLength(500);
    }
}

internal sealed class ApproveLeaveRequestHandler(
    IApplicationDbContext db,
    ICurrentUser user,
    TimeProvider clock,
    INotificationDispatcher notifications)
    : IRequestHandler<ApproveLeaveRequestCommand, Result>
{
    public async Task<Result> Handle(ApproveLeaveRequestCommand cmd, CancellationToken ct)
    {
        var req = await db.LeaveRequests.FirstOrDefaultAsync(r => r.Id == cmd.RequestId, ct);
        if (req is null) return Result.NotFound();

        if (req.Status != LeaveRequestStatus.Pending)
            return Result.Invalid("status", $"Cannot approve a request that is {req.Status}.");

        var balance = await db.LeaveBalances.FirstOrDefaultAsync(b =>
            b.EmployeeId  == req.EmployeeId &&
            b.LeaveTypeId == req.LeaveTypeId &&
            b.PeriodYear  == req.StartDate.Year, ct);

        if (balance is null)
            return Result.Invalid("balance", "Leave balance row missing.");

        // The real invariant we care about is "enough remaining entitlement"
        // (Entitlement + CarryOver − Used ≥ DaysRequested). Pending is just
        // accounting plumbing — it can fall out of sync when a request was
        // created by means other than FileLeaveRequestCommand (seeded demo
        // data, bulk imports, direct DB inserts). Clamp it at 0 when moving
        // days to Used so the approval doesn't fail on a bookkeeping mismatch.
        var remaining = balance.Entitlement + balance.CarryOver - balance.Used;
        if (remaining < req.DaysRequested)
            return Result.Invalid("balance",
                $"Insufficient leave balance. Available: {remaining}, requested: {req.DaysRequested}.");

        balance.Pending = Math.Max(0m, balance.Pending - req.DaysRequested);
        balance.Used   += req.DaysRequested;

        req.Status         = LeaveRequestStatus.Approved;
        req.ResolvedById   = user.Id;
        req.ResolvedAt     = clock.GetUtcNow();
        req.ResolutionNote = cmd.Note;

        // Domain event → outbox dispatcher will pick it up
        req.Raise(new LeaveRequestApproved(req.Id, req.EmployeeId, user.Id));

        // Notify the filer in-app. Look up their user record via Employee
        // (filer.EmployeeId == req.EmployeeId).
        var filerUserId = await db.Users
            .Where(u => u.EmployeeId == req.EmployeeId)
            .Select(u => (Guid?)u.Id)
            .FirstOrDefaultAsync(ct);
        if (filerUserId is { } uid)
        {
            await notifications.NotifyUserAsync(
                recipientUserId:   uid,
                type:              NotificationType.LeaveApproved,
                title:             "Leave approved",
                body:              $"Your leave request for {req.StartDate:MMM d} – {req.EndDate:MMM d} was approved.",
                relatedEntityId:   req.Id,
                relatedEntityType: "LeaveRequest",
                ct: ct);
        }

        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
