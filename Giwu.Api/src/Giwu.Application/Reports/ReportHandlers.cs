using Giwu.Application.Common;
using Giwu.Contracts.Reports;
using Giwu.Domain.Attendance;
using Giwu.Domain.Leaves;
using Giwu.Domain.Organization;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Giwu.Application.Reports;

// ── Headcount ───────────────────────────────────────────────────────────────
public sealed record HeadcountSummaryQuery() : IRequest<Result<HeadcountSummaryDto>>;

internal sealed class HeadcountSummaryHandler(IApplicationDbContext db)
    : IRequestHandler<HeadcountSummaryQuery, Result<HeadcountSummaryDto>>
{
    public async Task<Result<HeadcountSummaryDto>> Handle(HeadcountSummaryQuery q, CancellationToken ct)
    {
        var emps = await db.Employees
            .Select(e => new { e.Id, e.Status, e.DepartmentId })
            .ToListAsync(ct);

        var byStatus = emps.GroupBy(e => e.Status).ToDictionary(g => g.Key, g => g.Count());

        var deptCounts = await (from d in db.Departments
                                select new HeadcountByDepartmentDto(
                                    d.Id, d.Name,
                                    db.Employees.Count(e => e.DepartmentId == d.Id)))
                                .OrderByDescending(x => x.Count)
                                .ToListAsync(ct);

        return Result<HeadcountSummaryDto>.Success(new HeadcountSummaryDto(
            TotalEmployees: emps.Count,
            Active:        byStatus.GetValueOrDefault(EmploymentStatus.Active),
            OnLeave:       byStatus.GetValueOrDefault(EmploymentStatus.OnLeave),
            Suspended:     byStatus.GetValueOrDefault(EmploymentStatus.Suspended),
            Terminated:    byStatus.GetValueOrDefault(EmploymentStatus.Terminated),
            Resigned:      byStatus.GetValueOrDefault(EmploymentStatus.Resigned),
            Departments:   deptCounts.Count,
            ByDepartment:  deptCounts));
    }
}

// ── Attendance ──────────────────────────────────────────────────────────────
public sealed record AttendanceSummaryQuery(DateOnly From, DateOnly To)
    : IRequest<Result<AttendanceSummaryDto>>;

internal sealed class AttendanceSummaryHandler(IApplicationDbContext db)
    : IRequestHandler<AttendanceSummaryQuery, Result<AttendanceSummaryDto>>
{
    public async Task<Result<AttendanceSummaryDto>> Handle(AttendanceSummaryQuery q, CancellationToken ct)
    {
        var rows = await db.AttendanceRecords
            .Where(a => a.Date >= q.From && a.Date <= q.To)
            .Select(a => new { a.Status, a.ClockIn, a.ClockOut })
            .ToListAsync(ct);

        var present = rows.Count(r => r.Status == AttendanceStatus.Present);
        var late    = rows.Count(r => r.Status == AttendanceStatus.Late);
        var absent  = rows.Count(r => r.Status == AttendanceStatus.Absent);
        var leave   = rows.Count(r => r.Status == AttendanceStatus.OnLeave);

        decimal totalHours = 0m;
        int hoursCount = 0;
        foreach (var r in rows)
        {
            if (r.ClockIn is { } ci && r.ClockOut is { } co && co > ci)
            {
                totalHours += (decimal)(co - ci).TotalHours;
                hoursCount++;
            }
        }
        var avgHours = hoursCount == 0 ? 0m : Math.Round(totalHours / hoursCount, 2);

        return Result<AttendanceSummaryDto>.Success(new AttendanceSummaryDto(
            q.From, q.To, rows.Count, present, late, absent, leave,
            Math.Round(totalHours, 2), avgHours));
    }
}

// ── Leave ───────────────────────────────────────────────────────────────────
public sealed record LeaveSummaryQuery() : IRequest<Result<LeaveSummaryDto>>;

internal sealed class LeaveSummaryHandler(IApplicationDbContext db, TimeProvider clock)
    : IRequestHandler<LeaveSummaryQuery, Result<LeaveSummaryDto>>
{
    public async Task<Result<LeaveSummaryDto>> Handle(LeaveSummaryQuery q, CancellationToken ct)
    {
        var requests = await db.LeaveRequests
            .Select(r => new { r.Id, r.Status, r.LeaveTypeId, r.DaysRequested, r.ResolvedAt })
            .ToListAsync(ct);

        var byStatus = requests.GroupBy(r => r.Status).ToDictionary(g => g.Key, g => g.Count());
        var now = clock.GetUtcNow();
        var monthDays = requests
            .Where(r => r.Status == LeaveRequestStatus.Approved && r.ResolvedAt.HasValue
                && r.ResolvedAt.Value.Year == now.Year && r.ResolvedAt.Value.Month == now.Month)
            .Sum(r => r.DaysRequested);

        var types = await db.LeaveTypes.Select(t => new { t.Id, t.Name, t.Code }).ToListAsync(ct);
        var byType = (from t in types
                      let typeReqs = requests.Where(r => r.LeaveTypeId == t.Id).ToList()
                      select new LeaveByTypeDto(
                          t.Id, t.Name, t.Code,
                          typeReqs.Count,
                          typeReqs.Where(r => r.Status == LeaveRequestStatus.Approved).Sum(r => r.DaysRequested)))
                     .OrderByDescending(x => x.RequestCount)
                     .ToList();

        return Result<LeaveSummaryDto>.Success(new LeaveSummaryDto(
            TotalRequests:        requests.Count,
            Pending:              byStatus.GetValueOrDefault(LeaveRequestStatus.Pending),
            Approved:             byStatus.GetValueOrDefault(LeaveRequestStatus.Approved),
            Rejected:             byStatus.GetValueOrDefault(LeaveRequestStatus.Rejected),
            Cancelled:            byStatus.GetValueOrDefault(LeaveRequestStatus.Cancelled),
            DaysApprovedThisMonth: monthDays,
            ByType:               byType));
    }
}

// ── Payroll ─────────────────────────────────────────────────────────────────
public sealed record PayrollSummaryQuery(int? Year) : IRequest<Result<PayrollSummaryDto>>;

internal sealed class PayrollSummaryHandler(IApplicationDbContext db)
    : IRequestHandler<PayrollSummaryQuery, Result<PayrollSummaryDto>>
{
    public async Task<Result<PayrollSummaryDto>> Handle(PayrollSummaryQuery q, CancellationToken ct)
    {
        var periodsQuery = db.PayPeriods.AsQueryable();
        if (q.Year.HasValue) periodsQuery = periodsQuery.Where(p => p.PeriodStart.Year == q.Year.Value);

        var periods = await periodsQuery
            .OrderBy(p => p.PeriodStart)
            .Select(p => new { p.Id, p.Code, p.PeriodStart, p.PeriodEnd })
            .ToListAsync(ct);

        var slips = await db.Payslips
            .Select(s => new
            {
                s.PayPeriodId,
                Gross = s.BasicSalary + s.Overtime + s.Bonus + s.Allowance,
                Deductions = s.Sss + s.PhilHealth + s.PagIbig + s.WithholdingTax + s.LoanDeduction + s.OtherDeduction,
            })
            .ToListAsync(ct);

        var byPeriod = periods.Select(p =>
        {
            var pSlips = slips.Where(s => s.PayPeriodId == p.Id).ToList();
            return new PayrollByPeriodDto(
                p.Id, p.Code, p.PeriodStart, p.PeriodEnd,
                pSlips.Sum(s => s.Gross),
                pSlips.Sum(s => s.Gross - s.Deductions));
        }).ToList();

        return Result<PayrollSummaryDto>.Success(new PayrollSummaryDto(
            PayPeriods:      periods.Count,
            Payslips:        slips.Count,
            TotalGross:      slips.Sum(s => s.Gross),
            TotalNet:        slips.Sum(s => s.Gross - s.Deductions),
            TotalDeductions: slips.Sum(s => s.Deductions),
            ByPeriod:        byPeriod));
    }
}
