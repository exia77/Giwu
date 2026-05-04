using Giwu.Application.Common;
using Giwu.Contracts.Attendance;
using Giwu.Contracts.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Giwu.Application.Attendance.Queries;

public sealed record ListAttendanceQuery(
    DateOnly? From = null,
    DateOnly? To = null,
    Guid? EmployeeId = null,
    int Page = 1,
    int PageSize = 50)
    : IRequest<Result<PagedResult<AttendanceRecordDto>>>;

internal sealed class ListAttendanceHandler(IApplicationDbContext db, ICurrentUser user)
    : IRequestHandler<ListAttendanceQuery, Result<PagedResult<AttendanceRecordDto>>>
{
    public async Task<Result<PagedResult<AttendanceRecordDto>>> Handle(
        ListAttendanceQuery q, CancellationToken ct)
    {
        var page = Math.Max(1, q.Page);
        var size = Math.Clamp(q.PageSize, 1, 500);

        var query = from r in db.AttendanceRecords
                    join e in db.Employees on r.EmployeeId equals e.Id
                    select new { r, e };

        // Self-only unless user has the broader perm
        var canSeeAll = user.Permissions.Contains("attendance.view.all");
        if (!canSeeAll && user.EmployeeId is { } eid)
            query = query.Where(x => x.r.EmployeeId == eid);
        else if (q.EmployeeId.HasValue)
            query = query.Where(x => x.r.EmployeeId == q.EmployeeId.Value);

        if (q.From.HasValue) query = query.Where(x => x.r.Date >= q.From.Value);
        if (q.To.HasValue)   query = query.Where(x => x.r.Date <= q.To.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.r.Date)
            .Skip((page - 1) * size).Take(size)
            .Select(x => new AttendanceRecordDto(
                x.r.Id, x.e.Id, x.e.FirstName + " " + x.e.LastName,
                x.r.Date, x.r.ClockIn, x.r.ClockOut, x.r.BreakMinutes,
                x.r.Status, x.r.LateMinutes, x.r.UndertimeMinutes,
                x.r.OvertimeApprovedMinutes, x.r.Notes))
            .ToListAsync(ct);

        return Result<PagedResult<AttendanceRecordDto>>.Success(
            new PagedResult<AttendanceRecordDto>(items, total, page, size));
    }
}

public sealed record GetTodayAttendanceQuery(Guid? EmployeeId = null)
    : IRequest<Result<AttendanceRecordDto?>>;

internal sealed class GetTodayAttendanceHandler(
    IApplicationDbContext db,
    ICurrentUser user,
    TimeProvider clock)
    : IRequestHandler<GetTodayAttendanceQuery, Result<AttendanceRecordDto?>>
{
    public async Task<Result<AttendanceRecordDto?>> Handle(GetTodayAttendanceQuery q, CancellationToken ct)
    {
        var empId = q.EmployeeId ?? user.EmployeeId;
        if (empId is null) return Result<AttendanceRecordDto?>.NotFound();

        if (empId != user.EmployeeId && !user.Permissions.Contains("attendance.view.all"))
            return Result<AttendanceRecordDto?>.Forbidden();

        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);

        var dto = await (
            from r in db.AttendanceRecords
            join e in db.Employees on r.EmployeeId equals e.Id
            where r.EmployeeId == empId && r.Date == today
            select new AttendanceRecordDto(
                r.Id, e.Id, e.FirstName + " " + e.LastName,
                r.Date, r.ClockIn, r.ClockOut, r.BreakMinutes,
                r.Status, r.LateMinutes, r.UndertimeMinutes,
                r.OvertimeApprovedMinutes, r.Notes)
        ).FirstOrDefaultAsync(ct);

        return Result<AttendanceRecordDto?>.Success(dto);
    }
}
