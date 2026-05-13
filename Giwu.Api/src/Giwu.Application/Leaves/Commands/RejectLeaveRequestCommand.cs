using FluentValidation;
using Giwu.Application.Common;
using Giwu.Application.Notifications;
using Giwu.Domain.Leaves;
using Giwu.Domain.Notifications;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Giwu.Application.Leaves.Commands;

public sealed record RejectLeaveRequestCommand(Guid RequestId, string Note) : IRequest<Result>;

public sealed class RejectLeaveRequestValidator : AbstractValidator<RejectLeaveRequestCommand>
{
    public RejectLeaveRequestValidator()
    {
        RuleFor(x => x.RequestId).NotEmpty();
        RuleFor(x => x.Note).NotEmpty().MaximumLength(500)
            .WithMessage("A reason is required when rejecting a leave request.");
    }
}

internal sealed class RejectLeaveRequestHandler(
    IApplicationDbContext db,
    ICurrentUser user,
    TimeProvider clock,
    INotificationDispatcher notifications)
    : IRequestHandler<RejectLeaveRequestCommand, Result>
{
    public async Task<Result> Handle(RejectLeaveRequestCommand cmd, CancellationToken ct)
    {
        var req = await db.LeaveRequests.FirstOrDefaultAsync(r => r.Id == cmd.RequestId, ct);
        if (req is null) return Result.NotFound();

        if (req.Status != LeaveRequestStatus.Pending)
            return Result.Invalid("status", $"Cannot reject a request that is {req.Status}.");

        var balance = await db.LeaveBalances.FirstOrDefaultAsync(b =>
            b.EmployeeId  == req.EmployeeId &&
            b.LeaveTypeId == req.LeaveTypeId &&
            b.PeriodYear  == req.StartDate.Year, ct);

        // Release the held balance back to Available
        if (balance is not null)
            balance.Pending = Math.Max(0m, balance.Pending - req.DaysRequested);

        req.Status         = LeaveRequestStatus.Rejected;
        req.ResolvedById   = user.Id;
        req.ResolvedAt     = clock.GetUtcNow();
        req.ResolutionNote = cmd.Note;

        var filerUserId = await db.Users
            .Where(u => u.EmployeeId == req.EmployeeId)
            .Select(u => (Guid?)u.Id)
            .FirstOrDefaultAsync(ct);
        if (filerUserId is { } uid)
        {
            await notifications.NotifyUserAsync(
                recipientUserId:   uid,
                type:              NotificationType.LeaveRejected,
                title:             "Leave rejected",
                body:              string.IsNullOrWhiteSpace(cmd.Note)
                                       ? $"Your leave for {req.StartDate:MMM d} – {req.EndDate:MMM d} was rejected."
                                       : $"Rejected: {cmd.Note}",
                relatedEntityId:   req.Id,
                relatedEntityType: "LeaveRequest",
                ct: ct);
        }

        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
