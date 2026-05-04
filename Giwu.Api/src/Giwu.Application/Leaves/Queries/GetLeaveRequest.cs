using Giwu.Application.Common;
using Giwu.Contracts.Leaves;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Giwu.Application.Leaves.Queries;

public sealed record GetLeaveRequestQuery(Guid Id) : IRequest<Result<LeaveRequestDto>>;

internal sealed class GetLeaveRequestHandler(IApplicationDbContext db)
    : IRequestHandler<GetLeaveRequestQuery, Result<LeaveRequestDto>>
{
    public async Task<Result<LeaveRequestDto>> Handle(GetLeaveRequestQuery q, CancellationToken ct)
    {
        var dto = await (
            from r in db.LeaveRequests
            join e in db.Employees on r.EmployeeId equals e.Id
            join t in db.LeaveTypes on r.LeaveTypeId equals t.Id
            where r.Id == q.Id
            select new LeaveRequestDto(
                r.Id, e.Id, e.FirstName + " " + e.LastName,
                t.Id, t.Name, t.Code,
                r.StartDate, r.EndDate, r.IsHalfDay, r.DaysRequested,
                r.Reason, r.Status, r.CreatedAt)
        ).FirstOrDefaultAsync(ct);

        return dto is null
            ? Result<LeaveRequestDto>.NotFound()
            : Result<LeaveRequestDto>.Success(dto);
    }
}

public sealed record ListLeaveBalancesQuery(Guid? EmployeeId = null, int? Year = null)
    : IRequest<Result<IReadOnlyList<LeaveBalanceDto>>>;

internal sealed class ListLeaveBalancesHandler(IApplicationDbContext db, ICurrentUser user)
    : IRequestHandler<ListLeaveBalancesQuery, Result<IReadOnlyList<LeaveBalanceDto>>>
{
    public async Task<Result<IReadOnlyList<LeaveBalanceDto>>> Handle(
        ListLeaveBalancesQuery q, CancellationToken ct)
    {
        var year = q.Year ?? DateTime.UtcNow.Year;

        // Default scope: own balances only, unless caller has the perm to view others.
        var empId = q.EmployeeId;
        if (empId is null)
        {
            if (user.EmployeeId is null) return Result<IReadOnlyList<LeaveBalanceDto>>.NotFound();
            empId = user.EmployeeId;
        }
        else if (empId != user.EmployeeId && !user.Permissions.Contains("leave.request.view.all"))
        {
            return Result<IReadOnlyList<LeaveBalanceDto>>.Forbidden();
        }

        var items = await (
            from b in db.LeaveBalances
            join t in db.LeaveTypes on b.LeaveTypeId equals t.Id
            where b.EmployeeId == empId && b.PeriodYear == year
            orderby t.Name
            select new LeaveBalanceDto(
                b.EmployeeId, t.Id, t.Name, t.Code, b.PeriodYear,
                b.Entitlement, b.CarryOver, b.Used, b.Pending,
                b.Entitlement + b.CarryOver - b.Used - b.Pending)
        ).ToListAsync(ct);

        return Result<IReadOnlyList<LeaveBalanceDto>>.Success(items);
    }
}
