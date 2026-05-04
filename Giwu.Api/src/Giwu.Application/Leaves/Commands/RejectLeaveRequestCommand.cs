using FluentValidation;
using Giwu.Application.Common;
using Giwu.Domain.Leaves;
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
    TimeProvider clock)
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

        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
