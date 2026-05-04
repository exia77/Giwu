using Giwu.Application.Common;
using Giwu.Contracts.Common;
using Giwu.Contracts.Leaves;
using Giwu.Domain.Leaves;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Giwu.Application.Leaves.Queries;

public sealed record ListLeaveRequestsQuery(
    int Page = 1,
    int PageSize = 25,
    LeaveRequestStatus? Status = null,
    Guid? EmployeeId = null,
    bool MineOnly = false)
    : IRequest<Result<PagedResult<LeaveRequestDto>>>;

internal sealed class ListLeaveRequestsHandler(
    IApplicationDbContext db,
    ICurrentUser user)
    : IRequestHandler<ListLeaveRequestsQuery, Result<PagedResult<LeaveRequestDto>>>
{
    public async Task<Result<PagedResult<LeaveRequestDto>>> Handle(
        ListLeaveRequestsQuery q, CancellationToken ct)
    {
        var page = Math.Max(1, q.Page);
        var size = Math.Clamp(q.PageSize, 1, 200);

        var query = from r in db.LeaveRequests
                    join e in db.Employees on r.EmployeeId equals e.Id
                    join t in db.LeaveTypes on r.LeaveTypeId equals t.Id
                    select new { r, e, t };

        if (q.Status.HasValue)
            query = query.Where(x => x.r.Status == q.Status.Value);

        if (q.MineOnly && user.EmployeeId is { } eid)
            query = query.Where(x => x.r.EmployeeId == eid);
        else if (q.EmployeeId.HasValue)
            query = query.Where(x => x.r.EmployeeId == q.EmployeeId.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.r.CreatedAt)
            .Skip((page - 1) * size).Take(size)
            .Select(x => new LeaveRequestDto(
                x.r.Id, x.e.Id, x.e.FirstName + " " + x.e.LastName,
                x.t.Id, x.t.Name, x.t.Code,
                x.r.StartDate, x.r.EndDate, x.r.IsHalfDay, x.r.DaysRequested,
                x.r.Reason, x.r.Status, x.r.CreatedAt))
            .ToListAsync(ct);

        return Result<PagedResult<LeaveRequestDto>>.Success(
            new PagedResult<LeaveRequestDto>(items, total, page, size));
    }
}
