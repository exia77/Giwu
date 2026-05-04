using FluentValidation;
using Giwu.Application.Common;
using Giwu.Domain.Leaves;
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
    TimeProvider clock)
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

        if (balance.Pending < req.DaysRequested)
            return Result.Invalid("balance", "Pending balance is out of sync — refile required.");

        // Move days from Pending → Used
        balance.Pending -= req.DaysRequested;
        balance.Used    += req.DaysRequested;

        req.Status         = LeaveRequestStatus.Approved;
        req.ResolvedById   = user.Id;
        req.ResolvedAt     = clock.GetUtcNow();
        req.ResolutionNote = cmd.Note;

        // Domain event → outbox dispatcher will pick it up
        req.Raise(new LeaveRequestApproved(req.Id, req.EmployeeId, user.Id));

        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
