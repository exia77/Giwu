using FluentValidation;
using Giwu.Application.Common;
using Giwu.Application.Notifications;
using Giwu.Contracts.Attendance;
using Giwu.Domain.Attendance;
using Giwu.Domain.Notifications;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Giwu.Application.Attendance.Overtime;

// ── List ────────────────────────────────────────────────────────────────────
public sealed record ListOvertimeRequestsQuery(ApprovalStatus? Status = null, Guid? EmployeeId = null, bool MineOnly = false)
    : IRequest<Result<IReadOnlyList<OvertimeRequestDto>>>;

internal sealed class ListOvertimeRequestsHandler(IApplicationDbContext db, ICurrentUser user)
    : IRequestHandler<ListOvertimeRequestsQuery, Result<IReadOnlyList<OvertimeRequestDto>>>
{
    public async Task<Result<IReadOnlyList<OvertimeRequestDto>>> Handle(
        ListOvertimeRequestsQuery q, CancellationToken ct)
    {
        var query = from o in db.OvertimeRequests
                    join e in db.Employees on o.EmployeeId equals e.Id
                    select new { o, e };

        if (q.Status.HasValue)            query = query.Where(x => x.o.Status == q.Status.Value);
        if (q.MineOnly && user.EmployeeId is { } eid) query = query.Where(x => x.o.EmployeeId == eid);
        else if (q.EmployeeId.HasValue)   query = query.Where(x => x.o.EmployeeId == q.EmployeeId.Value);

        var items = await query
            .OrderByDescending(x => x.o.CreatedAt)
            .Select(x => new OvertimeRequestDto(
                x.o.Id, x.e.Id, x.e.FirstName + " " + x.e.LastName,
                x.o.Date, x.o.StartTime, x.o.EndTime, x.o.Type, x.o.Reason,
                x.o.Status, x.o.CreatedAt))
            .ToListAsync(ct);

        return Result<IReadOnlyList<OvertimeRequestDto>>.Success(items);
    }
}

// ── File ────────────────────────────────────────────────────────────────────
public sealed record FileOvertimeCommand(FileOvertimeRequest Request)
    : IRequest<Result<OvertimeRequestDto>>;

public sealed class FileOvertimeValidator : AbstractValidator<FileOvertimeCommand>
{
    public FileOvertimeValidator()
    {
        RuleFor(x => x.Request.StartTime).LessThan(x => x.Request.EndTime)
            .WithMessage("Start time must be before end time.");
        RuleFor(x => x.Request.Reason).NotEmpty().MaximumLength(500);
    }
}

internal sealed class FileOvertimeHandler(
    IApplicationDbContext db, ICurrentUser user, INotificationDispatcher notifications)
    : IRequestHandler<FileOvertimeCommand, Result<OvertimeRequestDto>>
{
    public async Task<Result<OvertimeRequestDto>> Handle(FileOvertimeCommand cmd, CancellationToken ct)
    {
        if (user.EmployeeId is null)
            return Result<OvertimeRequestDto>.Forbidden("Only employees can file OT requests.");

        var emp = await db.Employees.FirstOrDefaultAsync(e => e.Id == user.EmployeeId, ct);
        if (emp is null) return Result<OvertimeRequestDto>.NotFound();

        var r = cmd.Request;
        var ot = new OvertimeRequest
        {
            EmployeeId = emp.Id,
            Date = r.Date, StartTime = r.StartTime, EndTime = r.EndTime,
            Type = r.Type, Reason = r.Reason, Status = ApprovalStatus.Pending,
        };
        db.OvertimeRequests.Add(ot);

        await notifications.NotifyRoleAsync(
            roleName: Giwu.Domain.Identity.SystemRoles.HrAdmin,
            type:     NotificationType.OvertimeFiled,
            title:    $"Overtime from {emp.FirstName} {emp.LastName}",
            body:     $"{ot.Date:MMM d} · {ot.StartTime:HH:mm}–{ot.EndTime:HH:mm} ({ot.Type})",
            relatedEntityId:   ot.Id,
            relatedEntityType: "OvertimeRequest",
            ct: ct);

        await db.SaveChangesAsync(ct);

        return Result<OvertimeRequestDto>.Success(new OvertimeRequestDto(
            ot.Id, emp.Id, emp.FirstName + " " + emp.LastName,
            ot.Date, ot.StartTime, ot.EndTime, ot.Type, ot.Reason,
            ot.Status, ot.CreatedAt));
    }
}

// ── Approve / Reject ────────────────────────────────────────────────────────
public sealed record ResolveOvertimeCommand(Guid Id, bool Approve, string Note) : IRequest<Result>;

internal sealed class ResolveOvertimeHandler(
    IApplicationDbContext db,
    ICurrentUser user,
    TimeProvider clock,
    INotificationDispatcher notifications)
    : IRequestHandler<ResolveOvertimeCommand, Result>
{
    public async Task<Result> Handle(ResolveOvertimeCommand cmd, CancellationToken ct)
    {
        var ot = await db.OvertimeRequests.FirstOrDefaultAsync(x => x.Id == cmd.Id, ct);
        if (ot is null) return Result.NotFound();

        if (ot.Status != ApprovalStatus.Pending)
            return Result.Invalid("status", $"Cannot resolve a request that is {ot.Status}.");

        ot.Status         = cmd.Approve ? ApprovalStatus.Approved : ApprovalStatus.Rejected;
        ot.ApprovedById   = user.Id;
        ot.ApprovedAt     = clock.GetUtcNow();
        ot.ResolutionNote = cmd.Note;

        // Notify the filer of the decision.
        var filerUserId = await db.Users
            .Where(u => u.EmployeeId == ot.EmployeeId)
            .Select(u => (Guid?)u.Id)
            .FirstOrDefaultAsync(ct);
        if (filerUserId is { } uid)
        {
            await notifications.NotifyUserAsync(
                recipientUserId:   uid,
                type:              cmd.Approve ? NotificationType.OvertimeApproved : NotificationType.OvertimeRejected,
                title:             cmd.Approve ? "Overtime approved" : "Overtime rejected",
                body:              $"Your OT for {ot.Date:MMM d} ({ot.StartTime:HH:mm}–{ot.EndTime:HH:mm}) was {(cmd.Approve ? "approved" : "rejected")}.",
                relatedEntityId:   ot.Id,
                relatedEntityType: "OvertimeRequest",
                ct: ct);
        }

        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
