using Giwu.Application.Common;
using Giwu.Domain.Leaves;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Giwu.Application.Leaves.Commands;

public sealed record CancelLeaveRequestCommand(Guid RequestId) : IRequest<Result>;

internal sealed class CancelLeaveRequestHandler(
    IApplicationDbContext db,
    ICurrentUser user,
    TimeProvider clock)
    : IRequestHandler<CancelLeaveRequestCommand, Result>
{
    public async Task<Result> Handle(CancelLeaveRequestCommand cmd, CancellationToken ct)
    {
        var req = await db.LeaveRequests.FirstOrDefaultAsync(r => r.Id == cmd.RequestId, ct);
        if (req is null) return Result.NotFound();

        // Only the requester or someone with manage permission can cancel
        var isOwner    = user.EmployeeId == req.EmployeeId;
        var canManage  = user.Permissions.Contains("leave.manage");
        if (!isOwner && !canManage) return Result.Forbidden();

        if (req.Status != LeaveRequestStatus.Pending)
            return Result.Invalid("status", $"Only pending requests can be cancelled (current: {req.Status}).");

        // Release the held balance
        var balance = await db.LeaveBalances.FirstOrDefaultAsync(b =>
            b.EmployeeId == req.EmployeeId &&
            b.LeaveTypeId == req.LeaveTypeId &&
            b.PeriodYear == req.StartDate.Year, ct);

        if (balance is not null)
            balance.Pending = Math.Max(0m, balance.Pending - req.DaysRequested);

        req.Status       = LeaveRequestStatus.Cancelled;
        req.ResolvedById = user.Id;
        req.ResolvedAt   = clock.GetUtcNow();

        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
