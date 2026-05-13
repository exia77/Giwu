using Giwu.Application.Common;
using Giwu.Contracts.Attendance;
using Giwu.Domain.Attendance;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Giwu.Application.Attendance.Commands;

public sealed record ClockInCommand(ClockInRequest Request) : IRequest<Result<AttendanceRecordDto>>;

internal sealed class ClockInHandler(
    IApplicationDbContext db,
    ICurrentUser user,
    TimeProvider clock)
    : IRequestHandler<ClockInCommand, Result<AttendanceRecordDto>>
{
    public async Task<Result<AttendanceRecordDto>> Handle(ClockInCommand cmd, CancellationToken ct)
    {
        if (user.EmployeeId is null)
            return Result<AttendanceRecordDto>.Forbidden("Only employees can clock in.");

        var emp = await db.Employees.FirstOrDefaultAsync(e => e.Id == user.EmployeeId, ct);
        if (emp is null) return Result<AttendanceRecordDto>.NotFound("Employee profile not found");

        var now = clock.GetUtcNow();
        var today = DateOnly.FromDateTime(now.UtcDateTime);

        var record = await db.AttendanceRecords.FirstOrDefaultAsync(
            r => r.EmployeeId == emp.Id && r.Date == today, ct);

        if (record is not null && record.ClockIn is not null)
            return Result<AttendanceRecordDto>.Conflict($"Already clocked in at {record.ClockIn:HH:mm}.");

        if (record is null)
        {
            record = new AttendanceRecord
            {
                EmployeeId = emp.Id,
                Date = today,
                Status = AttendanceStatus.Present,
            };
            db.AttendanceRecords.Add(record);
        }

        record.ClockIn = now;
        record.ClockInSource = ParseSource(cmd.Request.Source);
        record.ClockInLocation = cmd.Request.Location;

        await db.SaveChangesAsync(ct);

        return Result<AttendanceRecordDto>.Success(new AttendanceRecordDto(
            record.Id, emp.Id, emp.FirstName + " " + emp.LastName,
            record.Date, record.ClockIn, record.ClockOut, record.BreakMinutes,
            record.Status, record.LateMinutes, record.UndertimeMinutes,
            record.OvertimeApprovedMinutes, record.Notes));
    }

    internal static AttendanceSource ParseSource(string s) =>
        Enum.TryParse<AttendanceSource>(s, ignoreCase: true, out var v) ? v : AttendanceSource.App;
}

public sealed record ClockOutCommand(ClockOutRequest Request) : IRequest<Result<AttendanceRecordDto>>;

internal sealed class ClockOutHandler(
    IApplicationDbContext db,
    ICurrentUser user,
    TimeProvider clock)
    : IRequestHandler<ClockOutCommand, Result<AttendanceRecordDto>>
{
    public async Task<Result<AttendanceRecordDto>> Handle(ClockOutCommand cmd, CancellationToken ct)
    {
        if (user.EmployeeId is null)
            return Result<AttendanceRecordDto>.Forbidden("Only employees can clock out.");

        var emp = await db.Employees.FirstOrDefaultAsync(e => e.Id == user.EmployeeId, ct);
        if (emp is null) return Result<AttendanceRecordDto>.NotFound("Employee profile not found");

        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);

        var record = await db.AttendanceRecords.FirstOrDefaultAsync(
            r => r.EmployeeId == emp.Id && r.Date == today, ct);

        if (record is null || record.ClockIn is null)
            return Result<AttendanceRecordDto>.NotFound("No active clock-in for today.");

        if (record.ClockOut is not null)
            return Result<AttendanceRecordDto>.Conflict($"Already clocked out at {record.ClockOut:HH:mm}.");

        record.ClockOut = clock.GetUtcNow();
        record.ClockOutSource = ClockInHandler.ParseSource(cmd.Request.Source);
        record.ClockOutLocation = cmd.Request.Location;
        if (cmd.Request.BreakMinutes is { } b) record.BreakMinutes = b;

        await db.SaveChangesAsync(ct);

        return Result<AttendanceRecordDto>.Success(new AttendanceRecordDto(
            record.Id, emp.Id, emp.FirstName + " " + emp.LastName,
            record.Date, record.ClockIn, record.ClockOut, record.BreakMinutes,
            record.Status, record.LateMinutes, record.UndertimeMinutes,
            record.OvertimeApprovedMinutes, record.Notes));
    }
}

public sealed record UpdateAttendanceCommand(Guid Id, UpdateAttendanceRequest Request) : IRequest<Result<AttendanceRecordDto>>;

internal sealed class UpdateAttendanceHandler(IApplicationDbContext db)
    : IRequestHandler<UpdateAttendanceCommand, Result<AttendanceRecordDto>>
{
    public async Task<Result<AttendanceRecordDto>> Handle(UpdateAttendanceCommand cmd, CancellationToken ct)
    {
        var record = await db.AttendanceRecords.FirstOrDefaultAsync(r => r.Id == cmd.Id, ct);
        if (record is null) return Result<AttendanceRecordDto>.NotFound("Attendance record not found");

        var emp = await db.Employees.FirstOrDefaultAsync(e => e.Id == record.EmployeeId, ct);
        if (emp is null) return Result<AttendanceRecordDto>.NotFound("Employee not found");

        record.Status = cmd.Request.Status;
        record.Notes  = cmd.Request.Notes ?? "";

        // TimeOnly + record.Date → DateTimeOffset. Null clears the value.
        // Stored as UTC offset; clients are responsible for any TZ conversion
        // when binding the time field to local "HH:mm".
        record.ClockIn  = ToDtoFromTime(record.Date, cmd.Request.ClockInTime);
        record.ClockOut = ToDtoFromTime(record.Date, cmd.Request.ClockOutTime);

        await db.SaveChangesAsync(ct);

        return Result<AttendanceRecordDto>.Success(new AttendanceRecordDto(
            record.Id, emp.Id, emp.FirstName + " " + emp.LastName,
            record.Date, record.ClockIn, record.ClockOut, record.BreakMinutes,
            record.Status, record.LateMinutes, record.UndertimeMinutes,
            record.OvertimeApprovedMinutes, record.Notes));
    }

    private static DateTimeOffset? ToDtoFromTime(DateOnly date, TimeOnly? time)
    {
        if (time is null) return null;
        return new DateTimeOffset(date.ToDateTime(time.Value), TimeSpan.Zero);
    }
}

public sealed record ManualAttendanceEntryCommand(ManualAttendanceEntryRequest Request) : IRequest<Result<AttendanceRecordDto>>;

internal sealed class ManualAttendanceEntryHandler(IApplicationDbContext db)
    : IRequestHandler<ManualAttendanceEntryCommand, Result<AttendanceRecordDto>>
{
    public async Task<Result<AttendanceRecordDto>> Handle(ManualAttendanceEntryCommand cmd, CancellationToken ct)
    {
        var r = cmd.Request;
        var emp = await db.Employees.FirstOrDefaultAsync(e => e.Id == r.EmployeeId, ct);
        if (emp is null) return Result<AttendanceRecordDto>.NotFound("Employee not found");

        // Upsert by (EmployeeId, Date): if a record already exists for that day,
        // overwrite it; otherwise create one. This lets HR back-fill missed
        // clock-ins idempotently without surfacing a "record exists" error.
        var record = await db.AttendanceRecords.FirstOrDefaultAsync(
            x => x.EmployeeId == r.EmployeeId && x.Date == r.Date, ct);

        if (record is null)
        {
            record = new AttendanceRecord { EmployeeId = r.EmployeeId, Date = r.Date };
            db.AttendanceRecords.Add(record);
        }

        record.Status   = r.Status;
        record.Notes    = r.Notes ?? "";
        record.ClockIn  = r.ClockInTime  is { } ci ? new DateTimeOffset(r.Date.ToDateTime(ci), TimeSpan.Zero) : null;
        record.ClockOut = r.ClockOutTime is { } co ? new DateTimeOffset(r.Date.ToDateTime(co), TimeSpan.Zero) : null;
        record.ClockInSource = AttendanceSource.Manual;

        await db.SaveChangesAsync(ct);

        return Result<AttendanceRecordDto>.Success(new AttendanceRecordDto(
            record.Id, emp.Id, emp.FirstName + " " + emp.LastName,
            record.Date, record.ClockIn, record.ClockOut, record.BreakMinutes,
            record.Status, record.LateMinutes, record.UndertimeMinutes,
            record.OvertimeApprovedMinutes, record.Notes));
    }
}
