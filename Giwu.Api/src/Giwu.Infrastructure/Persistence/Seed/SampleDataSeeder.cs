using Giwu.Application.Common;
using Giwu.Domain.Attendance;
using Giwu.Domain.Benefits;
using Giwu.Domain.Common;
using Giwu.Domain.Identity;
using Giwu.Domain.Leaves;
using Giwu.Domain.Organization;
using Giwu.Domain.Payroll;
using Giwu.Domain.Recruitment;
using Giwu.Domain.Reports;
using Microsoft.EntityFrameworkCore;

namespace Giwu.Infrastructure.Persistence.Seed;

/// <summary>
/// Optional sample dataset for demo / dev. Loads a small, realistic, and
/// internally-consistent set of departments, leave types, employees, leave
/// balances, leave requests, and today's attendance. Strict invariants:
///   • Every Pending leave bumps <see cref="LeaveBalance.Pending"/>.
///   • Every Approved leave bumps <see cref="LeaveBalance.Used"/>.
///   • Every attendance row points at a real <see cref="Employee.Id"/>.
///   • Every leave balance row references an existing employee + leave type.
/// Idempotent: re-running on a populated DB is a no-op.
/// </summary>
public static class SampleDataSeeder
{
    public static async Task SeedAsync(
        ApplicationDbContext db,
        IPasswordHasher hasher,
        CancellationToken ct = default)
    {
        // Each module is independently idempotent — runs only when its primary
        // table is empty. Lets the seeder "top up" on a partially-seeded DB
        // without clobbering existing data.
        await SeedCorePeopleAsync(db, hasher, ct);

        var employees = await db.Employees.ToListAsync(ct);
        if (employees.Count == 0) return;

        await SeedAttendanceHistoryAsync(db, employees, ct);
        await SeedRecruitmentAsync(db, employees, ct);
        await SeedPayrollAsync(db, employees, ct);
        await SeedBenefitsAsync(db, employees, ct);
        await SeedReportsAsync(db, ct);

        // Demo users are seeded outside SeedCorePeopleAsync so that adding a new
        // role's demo account in code "tops up" already-seeded databases —
        // EnsureDemoUserAsync is itself idempotent (skips if email exists).
        await SeedDemoUsersAsync(db, hasher, employees, ct);
    }

    private static async Task SeedDemoUsersAsync(
        ApplicationDbContext db, IPasswordHasher hasher, List<Employee> employees, CancellationToken ct)
    {
        await EnsureDemoUserAsync(db, hasher, "hrspecialist@manna-hris.example", "ChangeMe!123",
            SystemRoles.HrSpecialist, employees.First(e => e.LastName == "Reyes").Id, "Demo HR Specialist", ct);
        await EnsureDemoUserAsync(db, hasher, "manager@manna-hris.example", "ChangeMe!123",
            SystemRoles.Manager, employees.First(e => e.LastName == "Aquino").Id, "Demo Manager", ct);
        await EnsureDemoUserAsync(db, hasher, "finance@manna-hris.example", "ChangeMe!123",
            SystemRoles.Finance, employees.First(e => e.LastName == "Gonzales").Id, "Demo Finance", ct);
        await EnsureDemoUserAsync(db, hasher, "employee@manna-hris.example", "ChangeMe!123",
            SystemRoles.Employee, employees.First(e => e.LastName == "Tolentino").Id, "Demo Employee", ct);

        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedCorePeopleAsync(
        ApplicationDbContext db,
        IPasswordHasher hasher,
        CancellationToken ct)
    {
        // Bail if any sample data already exists. We key on Employees because
        // that's the largest signal of a populated DB.
        if (await db.Employees.AnyAsync(ct)) return;

        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);

        // ── Departments ────────────────────────────────────────────────────
        var depts = await EnsureDepartmentsAsync(db, ct);

        // ── Leave types ────────────────────────────────────────────────────
        var leaveTypes = await EnsureLeaveTypesAsync(db, ct);
        var vl = leaveTypes.Single(t => t.Code == "VL");
        var sl = leaveTypes.Single(t => t.Code == "SL");
        var sil = leaveTypes.Single(t => t.Code == "SIL");

        // ── Employees ──────────────────────────────────────────────────────
        // Keep the list intentionally small (12) and predictable. One row per
        // (department × role) combination so the catalog feels realistic.
        var sampleEmployees = new (string First, string Last, string Title, string DeptCode, decimal Salary, int TenureYears, EmploymentStatus Status)[]
        {
            ("Maria",    "Santos",     "HR Director",               "HR",  120_000m, 8, EmploymentStatus.Active),
            ("Joshua",   "Reyes",      "HR Specialist",             "HR",   55_000m, 3, EmploymentStatus.Active),
            ("Andrea",   "Domingo",    "HR Generalist",             "HR",   52_000m, 2, EmploymentStatus.Active),

            ("Daniel",   "Aquino",     "Engineering Manager",       "ENG", 150_000m, 6, EmploymentStatus.Active),
            ("Patricia", "Dela Cruz",  "Senior Software Engineer",  "ENG",  95_000m, 5, EmploymentStatus.Active),
            ("Mark",     "Tolentino",  "Software Engineer",         "ENG",  70_000m, 2, EmploymentStatus.Active),
            ("Kevin",    "Bautista",   "QA Engineer",               "ENG",  60_000m, 1, EmploymentStatus.Active),
            ("Janine",   "Lara",       "Software Engineer",         "ENG",  66_000m, 1, EmploymentStatus.Active),

            ("Rosa",     "Mendoza",    "Finance Director",          "FIN", 130_000m, 7, EmploymentStatus.Active),
            ("Carlo",    "Lim",        "Senior Accountant",         "FIN",  72_000m, 4, EmploymentStatus.Active),
            ("Bea",      "Gonzales",   "Payroll Specialist",        "FIN",  55_000m, 2, EmploymentStatus.Active),

            ("Grace",    "Bituin",     "HR Generalist",             "HR",   50_000m, 2, EmploymentStatus.OnLeave),
        };

        var rng = new Random(1);
        var employees = new List<Employee>();
        var i = 1;
        foreach (var (first, last, title, deptCode, salary, tenure, status) in sampleEmployees)
        {
            var emp = new Employee
            {
                EmployeeNumber    = $"EMP-{1000 + i:D4}",
                FirstName         = first,
                LastName          = last,
                Email             = $"{first.ToLowerInvariant()}.{last.Replace(" ", "").ToLowerInvariant()}@manna-hris.example",
                PersonalEmail     = $"{first.ToLowerInvariant()}{last.Replace(" ", "").ToLowerInvariant()}@gmail.com",
                Phone             = $"+63 917 {rng.Next(100, 999)} {rng.Next(1000, 9999)}",
                BirthDate         = new DateOnly(1985 + rng.Next(0, 15), rng.Next(1, 13), rng.Next(1, 28)),
                Gender            = rng.Next(2) == 0 ? Gender.Male : Gender.Female,
                CivilStatus       = (CivilStatus)rng.Next(0, 3),
                DepartmentId      = depts[deptCode],
                JobTitle          = title,
                HireDate          = today.AddYears(-tenure).AddMonths(-rng.Next(0, 12)),
                RegularizationDate = today.AddYears(-tenure).AddMonths(-rng.Next(0, 12)).AddMonths(6),
                Status            = status,
                EmploymentType    = EmploymentType.Regular,
                WorkArrangement   = WorkArrangement.Onsite,
                MonthlyBaseSalary = salary,
                BankName          = "BPI",
                BankAccountNumber = $"1234-{rng.Next(1000, 9999)}-{rng.Next(10, 99)}",
                Tin               = $"{rng.Next(100, 999)}-{rng.Next(100, 999)}-{rng.Next(100, 999)}-000",
                PermanentAddress  = new Address { Line1 = $"{rng.Next(1, 999)} Mabini St", City = "Quezon City", Province = "Metro Manila", PostalCode = "1100" },
                CurrentAddress    = new Address { Line1 = $"{rng.Next(1, 999)} Mabini St", City = "Quezon City", Province = "Metro Manila", PostalCode = "1100" },
            };
            employees.Add(emp);
            db.Employees.Add(emp);
            i++;
        }
        await db.SaveChangesAsync(ct);

        // ── Leave balances ─────────────────────────────────────────────────
        // One row per (employee × active leave type) for the current year.
        // Used is randomized small; Pending is always 0 here — it'll be bumped
        // properly by the leave-request seed block below.
        var balanceByKey = new Dictionary<(Guid Emp, Guid Type), LeaveBalance>();
        foreach (var emp in employees)
        {
            foreach (var lt in leaveTypes)
            {
                var alreadyUsed = rng.Next(0, Math.Max(1, (int)lt.AnnualEntitlementDays / 3));
                var bal = new LeaveBalance
                {
                    EmployeeId  = emp.Id,
                    LeaveTypeId = lt.Id,
                    PeriodYear  = today.Year,
                    Entitlement = lt.AnnualEntitlementDays,
                    CarryOver   = 0,
                    Used        = alreadyUsed,
                    Pending     = 0,
                };
                db.LeaveBalances.Add(bal);
                balanceByKey[(emp.Id, lt.Id)] = bal;
            }
        }
        await db.SaveChangesAsync(ct);

        // ── Leave requests (with proper balance updates) ──────────────────
        // Pattern: 3 approved (past dates, bump Used), 2 pending (bump Pending).
        var hrDirector = employees.First(e => e.LastName == "Santos");
        (Employee Emp, LeaveType Type, int StartOffset, int Days, string Reason, LeaveRequestStatus Status)[] sampleLeaves =
        {
            (employees.First(e => e.LastName == "Reyes"),    sl,  -8,  2, "Flu",                  LeaveRequestStatus.Approved),
            (employees.First(e => e.LastName == "Lim"),      vl,  -20, 3, "Family event",         LeaveRequestStatus.Approved),
            (employees.First(e => e.LastName == "Tolentino"), vl, -45, 5, "Annual leave",         LeaveRequestStatus.Approved),
            (employees.First(e => e.LastName == "Bautista"), vl,  +5,  3, "Wedding attendance",   LeaveRequestStatus.Pending),
            (employees.First(e => e.LastName == "Lara"),     sl,  +1,  1, "Doctor appointment",   LeaveRequestStatus.Pending),
        };

        foreach (var (emp, type, startOffset, days, reason, status) in sampleLeaves)
        {
            var start = today.AddDays(startOffset);
            var end = start.AddDays(days - 1);

            db.LeaveRequests.Add(new LeaveRequest
            {
                EmployeeId    = emp.Id,
                LeaveTypeId   = type.Id,
                StartDate     = start,
                EndDate       = end,
                DaysRequested = days,
                Reason        = reason,
                Status        = status,
                ResolvedById  = status == LeaveRequestStatus.Approved ? hrDirector.Id : null,
                ResolvedAt    = status == LeaveRequestStatus.Approved
                    ? DateTimeOffset.UtcNow.AddDays(startOffset - 1)
                    : null,
                ResolutionNote = status == LeaveRequestStatus.Approved ? "Approved" : "",
            });

            // The reliability invariant: keep LeaveBalance in sync with whatever
            // state we're seeding. Approved → Used += days. Pending → Pending += days.
            var bal = balanceByKey[(emp.Id, type.Id)];
            if (status == LeaveRequestStatus.Approved) bal.Used    += days;
            else if (status == LeaveRequestStatus.Pending) bal.Pending += days;
        }
        await db.SaveChangesAsync(ct);

        // ── Today's attendance ─────────────────────────────────────────────
        // Mostly Present (clock-in at 8:00-ish). One Late row to exercise the
        // status badge. We skip OnLeave employees since they shouldn't have a
        // clock-in record today.
        var nowUtc = DateTimeOffset.UtcNow;
        var presentCount = 0;
        foreach (var emp in employees.Where(e => e.Status == EmploymentStatus.Active))
        {
            // Vary clock-in time so the table doesn't look like a stamp run.
            var minuteOffset = rng.Next(-5, 25); // -5 to +24 from 08:00
            var clockIn = new DateTimeOffset(today.ToDateTime(new TimeOnly(8, 0)).AddMinutes(minuteOffset), TimeSpan.Zero);
            var status = minuteOffset > 10 ? AttendanceStatus.Late : AttendanceStatus.Present;

            db.AttendanceRecords.Add(new AttendanceRecord
            {
                EmployeeId      = emp.Id,
                Date            = today,
                ClockIn         = clockIn,
                ClockInSource   = AttendanceSource.App,
                ClockInLocation = "HQ",
                Status          = status,
                Notes           = "",
            });
            presentCount++;
            if (presentCount >= 8) break; // keep today's attendance modest
        }

        // Demo users are seeded by SeedDemoUsersAsync (called from SeedAsync)
        // so adding a new role tops up already-seeded databases.

        await db.SaveChangesAsync(ct);
    }

    // ─── Attendance history backfill ────────────────────────────────────────

    private static async Task SeedAttendanceHistoryAsync(
        ApplicationDbContext db, List<Employee> employees, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var cutoff = today.AddDays(-7);
        if (await db.AttendanceRecords.AnyAsync(r => r.Date < cutoff, ct)) return;

        var active = employees.Where(e => e.Status == EmploymentStatus.Active).ToList();
        if (active.Count == 0) return;

        var rng = new Random(13);
        for (int dayOffset = -30; dayOffset < 0; dayOffset++)
        {
            var d = today.AddDays(dayOffset);
            if (d.DayOfWeek == DayOfWeek.Saturday || d.DayOfWeek == DayOfWeek.Sunday) continue;

            foreach (var emp in active)
            {
                // Skip if a row already exists for this employee+date (defensive
                // against the (EmployeeId, Date) unique index).
                var alreadyExists = await db.AttendanceRecords
                    .AnyAsync(r => r.EmployeeId == emp.Id && r.Date == d, ct);
                if (alreadyExists) continue;

                var roll = rng.NextDouble();
                if (roll < 0.04)
                {
                    db.AttendanceRecords.Add(new AttendanceRecord
                    {
                        EmployeeId = emp.Id, Date = d,
                        Status = AttendanceStatus.Absent,
                    });
                    continue;
                }

                var lateMin = roll < 0.14 ? rng.Next(11, 35) : rng.Next(-5, 8);
                var inAt  = new DateTimeOffset(d.ToDateTime(new TimeOnly(8, 0)).AddMinutes(lateMin), TimeSpan.Zero);
                var outAt = new DateTimeOffset(d.ToDateTime(new TimeOnly(17, 0)).AddMinutes(rng.Next(-10, 30)), TimeSpan.Zero);

                db.AttendanceRecords.Add(new AttendanceRecord
                {
                    EmployeeId = emp.Id, Date = d,
                    ClockIn = inAt, ClockOut = outAt,
                    ClockInSource = AttendanceSource.App,
                    ClockOutSource = AttendanceSource.App,
                    ClockInLocation = "HQ", ClockOutLocation = "HQ",
                    Status = lateMin > 10 ? AttendanceStatus.Late : AttendanceStatus.Present,
                    LateMinutes = lateMin > 10 ? lateMin : null,
                });
            }
        }
        await db.SaveChangesAsync(ct);
    }

    // ─── Recruitment ────────────────────────────────────────────────────────

    private static async Task SeedRecruitmentAsync(
        ApplicationDbContext db, List<Employee> employees, CancellationToken ct)
    {
        if (await db.JobRequisitions.AnyAsync(ct)) return;

        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var nowUtc = DateTimeOffset.UtcNow;
        var deptByCode = await db.Departments.ToDictionaryAsync(d => d.Code, d => d.Id, ct);
        if (deptByCode.Count == 0) return;

        var hrDirector  = employees.FirstOrDefault(e => e.LastName == "Santos")?.Id ?? employees[0].Id;
        var engManager  = employees.FirstOrDefault(e => e.LastName == "Aquino")?.Id ?? employees[0].Id;
        var finDirector = employees.FirstOrDefault(e => e.LastName == "Mendoza")?.Id ?? employees[0].Id;

        var reqSeed = new (string Title, string DeptCode, JobStatus Status, int Openings, int Filled, decimal Min, decimal Max, int PostedDaysAgo, int TargetDays, Guid Owner, string Desc)[]
        {
            ("Senior Backend Engineer", "ENG", JobStatus.Open,   2, 0,  90_000m, 130_000m, 14, 60, engManager,  "Build the next-gen API and own backend reliability."),
            ("Frontend Engineer",       "ENG", JobStatus.Open,   1, 0,  70_000m, 100_000m, 10, 45, engManager,  "MAUI + Blazor frontend, focus on accessibility."),
            ("HR Generalist",           "HR",  JobStatus.Open,   1, 0,  45_000m,  60_000m, 21, 30, hrDirector,  "Support day-to-day HR operations and onboarding."),
            ("Payroll Specialist",      "FIN", JobStatus.Draft,  1, 0,  50_000m,  70_000m,  0, 60, finDirector, "Run end-to-end Philippine payroll cycles."),
            ("QA Engineer",             "ENG", JobStatus.Draft,  1, 0,  55_000m,  80_000m,  0, 45, engManager,  "Testing strategy, automation, and quality gates."),
            ("Office Admin",            "HR",  JobStatus.Closed, 1, 1,  25_000m,  35_000m, 60, 30, hrDirector,  "Front-desk admin (position filled)."),
        };

        var reqs = new List<JobRequisition>();
        foreach (var r in reqSeed)
        {
            var req = new JobRequisition
            {
                Title = r.Title,
                DepartmentId = deptByCode[r.DeptCode],
                Location = "Quezon City",
                EmploymentType = "Full-time",
                Openings = r.Openings,
                Filled = r.Filled,
                Status = r.Status,
                OwnerEmployeeId = r.Owner,
                SalaryMin = r.Min,
                SalaryMax = r.Max,
                PostedAt = r.Status == JobStatus.Draft ? default : nowUtc.AddDays(-r.PostedDaysAgo),
                TargetFillBy = today.AddDays(r.TargetDays),
                Description = r.Desc,
            };
            reqs.Add(req);
            db.JobRequisitions.Add(req);
        }
        await db.SaveChangesAsync(ct);

        // 15 candidates spread across the OPEN reqs (Draft/Closed reqs intentionally empty).
        var openReqs = reqs.Where(r => r.Status == JobStatus.Open).ToList();
        var cSeed = new (string First, string Last, string Email, string Source, int ReqIdx, CandidateStage Stage, int Rating, int DaysAgo)[]
        {
            ("Liam",     "Cruz",      "liam.cruz@example.com",        "LinkedIn",     0, CandidateStage.Interview, 4,  7),
            ("Sophia",   "Reyes",     "sophia.reyes@example.com",     "Referral",     0, CandidateStage.Screening, 5,  4),
            ("Noah",     "Garcia",    "noah.garcia@example.com",      "Indeed",       0, CandidateStage.Applied,   3,  2),
            ("Olivia",   "Mendoza",   "olivia.mendoza@example.com",   "LinkedIn",     0, CandidateStage.Rejected,  2, 11),
            ("James",    "Reyes",     "james.reyes@example.com",      "Referral",     0, CandidateStage.Hired,     5, 28),
            ("Ethan",    "Bautista",  "ethan.b@example.com",          "Career Fair",  1, CandidateStage.Offer,     5,  9),
            ("Ava",      "Domingo",   "ava.domingo@example.com",      "LinkedIn",     1, CandidateStage.Interview, 4,  6),
            ("Mason",    "Tolentino", "mason.t@example.com",          "Referral",     1, CandidateStage.Screening, 4,  5),
            ("Isabella", "Lim",       "isabella.lim@example.com",     "Indeed",       1, CandidateStage.Applied,   3,  2),
            ("Logan",    "Aquino",    "logan.aquino@example.com",     "Website",      1, CandidateStage.Rejected,  2, 14),
            ("Mia",      "Santos",    "mia.santos@example.com",       "LinkedIn",     2, CandidateStage.Interview, 5,  8),
            ("Lucas",    "Lara",      "lucas.lara@example.com",       "Referral",     2, CandidateStage.Screening, 4,  5),
            ("Charlotte","Gonzales",  "char.g@example.com",           "LinkedIn",     2, CandidateStage.Applied,   3,  3),
            ("Elijah",   "Dela Cruz", "elijah.dc@example.com",        "Career Fair",  2, CandidateStage.Applied,   3,  1),
            ("Amelia",   "Bituin",    "amelia.b@example.com",         "LinkedIn",     2, CandidateStage.Rejected,  2, 16),
        };

        var candidates = new List<Candidate>();
        foreach (var c in cSeed)
        {
            if (c.ReqIdx >= openReqs.Count) continue;
            var cand = new Candidate
            {
                JobRequisitionId = openReqs[c.ReqIdx].Id,
                FirstName = c.First,
                LastName  = c.Last,
                Email     = c.Email,
                Phone     = "+63 917 000 0000",
                Stage     = c.Stage,
                Source    = c.Source,
                Rating    = c.Rating,
                AppliedAt = nowUtc.AddDays(-c.DaysAgo),
                LastActivityAt = nowUtc.AddDays(-Math.Max(0, c.DaysAgo - 2)),
                RejectionReason = c.Stage == CandidateStage.Rejected ? "Not a fit for the role" : null,
            };
            candidates.Add(cand);
            db.Candidates.Add(cand);
        }
        await db.SaveChangesAsync(ct);

        // 6 interviews — anchored on candidates currently in the Interview stage.
        var interviewables = candidates.Where(c => c.Stage == CandidateStage.Interview).ToList();
        var interviewerPool = new[]
        {
            engManager, hrDirector, finDirector,
            employees.FirstOrDefault(e => e.LastName == "Dela Cruz")?.Id ?? engManager,
        };

        var ivSeed = new (int CandIdx, int DaysFromNow, InterviewKind Kind, int InterviewerIdx, InterviewStatus Status)[]
        {
            (0, -3, InterviewKind.PhoneScreen, 0, InterviewStatus.Completed),
            (0, +5, InterviewKind.Technical,   3, InterviewStatus.Scheduled),
            (1, +2, InterviewKind.PhoneScreen, 1, InterviewStatus.Scheduled),
            (1, +7, InterviewKind.Panel,       0, InterviewStatus.Scheduled),
            (2, -1, InterviewKind.PhoneScreen, 1, InterviewStatus.Completed),
            (2, +4, InterviewKind.Final,       0, InterviewStatus.Scheduled),
        };

        foreach (var iv in ivSeed)
        {
            if (iv.CandIdx >= interviewables.Count) continue;
            var when = DateTime.UtcNow.Date.AddDays(iv.DaysFromNow).AddHours(10);
            db.Interviews.Add(new Interview
            {
                CandidateId = interviewables[iv.CandIdx].Id,
                ScheduledAt = new DateTimeOffset(when, TimeSpan.Zero),
                DurationMinutes = iv.Kind == InterviewKind.PhoneScreen ? 30 : 60,
                Kind = iv.Kind,
                InterviewerEmployeeId = interviewerPool[iv.InterviewerIdx % interviewerPool.Length],
                Location = iv.Kind == InterviewKind.PhoneScreen ? "Phone" : "HQ Conference Room A",
                Status = iv.Status,
            });
        }
        await db.SaveChangesAsync(ct);
    }

    // ─── Payroll ────────────────────────────────────────────────────────────

    private static async Task SeedPayrollAsync(
        ApplicationDbContext db, List<Employee> employees, CancellationToken ct)
    {
        if (await db.PayPeriods.AnyAsync(ct)) return;

        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var nowUtc = DateTimeOffset.UtcNow;

        var currentStart = new DateOnly(today.Year, today.Month, 1);
        var oneAgoStart  = currentStart.AddMonths(-1);
        var twoAgoStart  = currentStart.AddMonths(-2);

        var periodSeed = new (DateOnly Start, DateOnly End, PayPeriodStatus Status, bool Released)[]
        {
            (twoAgoStart, oneAgoStart.AddDays(-1), PayPeriodStatus.Released,   true),
            (oneAgoStart, currentStart.AddDays(-1), PayPeriodStatus.Released,  true),
            (currentStart, currentStart.AddMonths(1).AddDays(-1), PayPeriodStatus.Calculated, false),
        };

        var hrDirector = employees.FirstOrDefault(e => e.LastName == "Santos")?.Id;
        var periods = new List<PayPeriod>();
        foreach (var p in periodSeed)
        {
            var period = new PayPeriod
            {
                Code = $"PP-{p.Start:yyyy-MM}",
                PeriodStart = p.Start,
                PeriodEnd = p.End,
                ReleaseDate = p.Released ? p.End.AddDays(2) : null,
                Frequency = "Monthly",
                Status = p.Status,
                ApprovedById = p.Released ? hrDirector : null,
                ApprovedAt = p.Released ? nowUtc.AddDays(-30) : null,
            };
            periods.Add(period);
            db.PayPeriods.Add(period);
        }
        await db.SaveChangesAsync(ct);

        // Payslip per active employee per period.
        var rng = new Random(7);
        var active = employees.Where(e => e.Status == EmploymentStatus.Active).ToList();

        foreach (var period in periods)
        {
            foreach (var emp in active)
            {
                var basic = emp.MonthlyBaseSalary;
                var overtime = period.Status == PayPeriodStatus.Released
                    ? Math.Round(basic * 0.05m * (decimal)rng.NextDouble(), 2)
                    : 0m;
                var allowance = 2_000m;

                // Rough PH statutory: SSS 4.5%, PhilHealth 4%, Pag-IBIG flat 100,
                // WHT ~20% on the slice above 25k. Demo-only — not BIR-accurate.
                var sss     = Math.Round(basic * 0.045m, 2);
                var phil    = Math.Round(basic * 0.04m, 2);
                var pagibig = 100m;
                var wht     = Math.Round(Math.Max(0m, basic - 25_000m) * 0.20m, 2);

                var status = period.Status switch
                {
                    PayPeriodStatus.Released  => PayslipStatus.Released,
                    PayPeriodStatus.Approved  => PayslipStatus.Approved,
                    _                         => PayslipStatus.Pending,
                };

                db.Payslips.Add(new Payslip
                {
                    PayPeriodId = period.Id,
                    EmployeeId  = emp.Id,
                    BasicSalary = basic,
                    Overtime    = overtime,
                    Bonus       = 0m,
                    Allowance   = allowance,
                    Sss            = sss,
                    PhilHealth     = phil,
                    PagIbig        = pagibig,
                    WithholdingTax = wht,
                    LoanDeduction  = 0m,
                    OtherDeduction = 0m,
                    Status = status,
                });
            }
        }
        await db.SaveChangesAsync(ct);
    }

    // ─── Benefits ───────────────────────────────────────────────────────────

    private static async Task SeedBenefitsAsync(
        ApplicationDbContext db, List<Employee> employees, CancellationToken ct)
    {
        if (await db.BenefitPrograms.AnyAsync(ct)) return;

        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var nowUtc = DateTimeOffset.UtcNow;

        var hmo = new BenefitProgram
        {
            Name = "Premium HMO Coverage",
            Provider = "Maxicare",
            Category = BenefitCategory.Health,
            Description = "Comprehensive medical + hospitalization (employee and dependents).",
            IsActive = true, IsMandatory = false,
            Eligibility = "Regular employees, after probation.",
            EffectiveDate = today.AddYears(-1),
            RenewalDate = today.AddMonths(3),
            MonthlyCostPerEmployee = 2_500m,
            EmployerShare = 100m, EmployeeShare = 0m, ShareIsPercent = true,
        };
        var life = new BenefitProgram
        {
            Name = "Group Life Insurance",
            Provider = "Sun Life",
            Category = BenefitCategory.Insurance,
            Description = "1x annual salary group life policy.",
            IsActive = true, IsMandatory = true,
            Eligibility = "All regular employees.",
            EffectiveDate = today.AddYears(-2),
            RenewalDate = today.AddMonths(6),
            MonthlyCostPerEmployee = 500m,
            EmployerShare = 100m, EmployeeShare = 0m, ShareIsPercent = true,
        };
        var meal = new BenefitProgram
        {
            Name = "Monthly Meal Allowance",
            Provider = "Internal",
            Category = BenefitCategory.Allowance,
            Description = "Non-taxable de minimis meal benefit (BIR-compliant cap).",
            IsActive = true, IsMandatory = true,
            Eligibility = "All regular employees.",
            EffectiveDate = today.AddYears(-1),
            MonthlyCostPerEmployee = 2_000m,
            EmployerShare = 100m, EmployeeShare = 0m, ShareIsPercent = true,
        };
        var training = new BenefitProgram
        {
            Name = "Training & Development Allowance",
            Provider = "Internal",
            Category = BenefitCategory.Education,
            Description = "Annual budget for courses, certifications, conferences.",
            IsActive = true, IsMandatory = false,
            Eligibility = "Regular employees, 1+ year tenure.",
            EffectiveDate = today.AddMonths(-6),
            MonthlyCostPerEmployee = 1_500m,
            EmployerShare = 80m, EmployeeShare = 20m, ShareIsPercent = true,
        };
        var loan = new BenefitProgram
        {
            Name = "Salary Loan Program",
            Provider = "Internal",
            Category = BenefitCategory.Loan,
            Description = "Short-term salary loan up to 2x monthly pay, payable over 12 months.",
            IsActive = true, IsMandatory = false,
            Eligibility = "Regular employees in good standing.",
            EffectiveDate = today.AddYears(-1),
            MonthlyCostPerEmployee = 0m,
            EmployerShare = 0m, EmployeeShare = 100m, ShareIsPercent = true,
        };

        db.BenefitPrograms.AddRange(hmo, life, meal, training, loan);
        await db.SaveChangesAsync(ct);

        var active = employees.Where(e => e.Status == EmploymentStatus.Active).ToList();
        var rng = new Random(11);

        foreach (var emp in active)
        {
            // Mandatory programs: everyone.
            db.BenefitEnrollments.Add(new BenefitEnrollment
            {
                EmployeeId = emp.Id, BenefitProgramId = life.Id,
                EnrolledOn = emp.HireDate, Status = EnrollmentStatus.Active,
                MonthlyContribution = life.MonthlyCostPerEmployee,
            });
            db.BenefitEnrollments.Add(new BenefitEnrollment
            {
                EmployeeId = emp.Id, BenefitProgramId = meal.Id,
                EnrolledOn = emp.HireDate, Status = EnrollmentStatus.Active,
                MonthlyContribution = meal.MonthlyCostPerEmployee,
            });
            // HMO: ~75% adoption.
            if (rng.NextDouble() < 0.75)
            {
                db.BenefitEnrollments.Add(new BenefitEnrollment
                {
                    EmployeeId = emp.Id, BenefitProgramId = hmo.Id,
                    EnrolledOn = emp.HireDate.AddMonths(6),
                    Status = EnrollmentStatus.Active,
                    MonthlyContribution = hmo.MonthlyCostPerEmployee,
                });
            }
            // Training: ~50% adoption.
            if (rng.NextDouble() < 0.5)
            {
                db.BenefitEnrollments.Add(new BenefitEnrollment
                {
                    EmployeeId = emp.Id, BenefitProgramId = training.Id,
                    EnrolledOn = today.AddMonths(-3),
                    Status = EnrollmentStatus.Active,
                    MonthlyContribution = training.MonthlyCostPerEmployee,
                });
            }
        }
        await db.SaveChangesAsync(ct);

        // A few BenefitRequests in different states so the page isn't empty.
        var byLast = active.ToDictionary(e => e.LastName, e => e);
        if (byLast.TryGetValue("Tolentino", out var loanReq))
        {
            db.BenefitRequests.Add(new BenefitRequest
            {
                EmployeeId = loanReq.Id, BenefitProgramId = loan.Id,
                Type = BenefitRequestType.Loan, Amount = 50_000m,
                RequestedAt = nowUtc.AddDays(-20), ResolvedAt = nowUtc.AddDays(-15),
                Status = BenefitRequestStatus.Repaying,
                Reason = "Home renovation",
                TermMonths = 12, MonthlyDeduction = 4_500m, OutstandingBalance = 35_000m,
            });
        }
        if (byLast.TryGetValue("Reyes", out var claimer))
        {
            db.BenefitRequests.Add(new BenefitRequest
            {
                EmployeeId = claimer.Id, BenefitProgramId = hmo.Id,
                Type = BenefitRequestType.Claim, Amount = 12_500m,
                RequestedAt = nowUtc.AddDays(-4),
                Status = BenefitRequestStatus.Pending,
                Reason = "Outpatient claim — dental procedure",
            });
        }
        if (byLast.TryGetValue("Bautista", out var reimb))
        {
            db.BenefitRequests.Add(new BenefitRequest
            {
                EmployeeId = reimb.Id, BenefitProgramId = training.Id,
                Type = BenefitRequestType.Reimbursement, Amount = 8_500m,
                RequestedAt = nowUtc.AddDays(-9), ResolvedAt = nowUtc.AddDays(-7),
                Status = BenefitRequestStatus.Approved,
                Reason = "AWS certification exam fee",
            });
        }
        if (byLast.TryGetValue("Lara", out var loan2))
        {
            db.BenefitRequests.Add(new BenefitRequest
            {
                EmployeeId = loan2.Id, BenefitProgramId = loan.Id,
                Type = BenefitRequestType.Loan, Amount = 30_000m,
                RequestedAt = nowUtc.AddDays(-2),
                Status = BenefitRequestStatus.Pending,
                Reason = "Family emergency",
                TermMonths = 6,
            });
        }
        await db.SaveChangesAsync(ct);
    }

    // ─── Reports + compliance ───────────────────────────────────────────────

    private static async Task SeedReportsAsync(ApplicationDbContext db, CancellationToken ct)
    {
        var hasDefs = await db.ReportDefinitions.AnyAsync(ct);
        var hasDeadlines = await db.ComplianceDeadlines.AnyAsync(ct);
        if (hasDefs && hasDeadlines) return;

        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var nowUtc = DateTime.UtcNow;

        if (!hasDefs)
        {
            db.ReportDefinitions.AddRange(
                new ReportDefinition
                {
                    Code = "RD-CUSTOM-001", Name = "Headcount by Department",
                    ShortDescription = "Active employee headcount grouped by department.",
                    Category = ReportCategory.People,
                    RequiresDateRange = false,
                    SupportedFormatsCsv = "Csv,Excel,Pdf",
                    ColumnsCsv = "Department,Active,OnLeave,Total",
                },
                new ReportDefinition
                {
                    Code = "RD-CUSTOM-002", Name = "Monthly Payroll Summary",
                    ShortDescription = "Gross, statutory, tax, and net per pay period.",
                    Category = ReportCategory.Payroll,
                    RequiresDateRange = true,
                    SupportedFormatsCsv = "Csv,Excel",
                    ColumnsCsv = "Period,Gross,Statutory,Tax,Net",
                },
                new ReportDefinition
                {
                    Code = "RD-CUSTOM-003", Name = "Attendance Exception Report",
                    ShortDescription = "Late, absent, undertime, and missing-clockout records.",
                    Category = ReportCategory.Attendance,
                    RequiresDateRange = true, RequiresDepartmentFilter = true,
                    SupportedFormatsCsv = "Csv,Excel",
                    ColumnsCsv = "Date,Employee,Status,LateMinutes,Notes",
                },
                new ReportDefinition
                {
                    Code = "RD-CUSTOM-004", Name = "Recruitment Pipeline",
                    ShortDescription = "Open reqs with candidate counts per stage.",
                    Category = ReportCategory.Recruitment,
                    RequiresDateRange = false,
                    SupportedFormatsCsv = "Csv,Excel,Pdf",
                    ColumnsCsv = "Requisition,Openings,Applied,Screening,Interview,Offer",
                },
                new ReportDefinition
                {
                    Code = "RD-CUSTOM-005", Name = "Benefits Enrollment Summary",
                    ShortDescription = "Active enrollments per program.",
                    Category = ReportCategory.Benefits,
                    RequiresDateRange = false,
                    SupportedFormatsCsv = "Csv,Excel",
                    ColumnsCsv = "Program,Active,Pending,Terminated",
                },
                new ReportDefinition
                {
                    Code = "RD-CUSTOM-006", Name = "Leave Balance Snapshot",
                    ShortDescription = "Per-employee entitlement / used / pending / available.",
                    Category = ReportCategory.Leave,
                    RequiresDateRange = false, RequiresDepartmentFilter = true,
                    SupportedFormatsCsv = "Csv,Excel",
                    ColumnsCsv = "Employee,Type,Entitlement,Used,Pending,Available",
                });

            // A pair of recurring schedules so the schedules tab isn't empty.
            db.ReportSchedules.AddRange(
                new ReportSchedule
                {
                    DefinitionId = "RD-CUSTOM-002", DefinitionName = "Monthly Payroll Summary",
                    Category = ReportCategory.Payroll,
                    Frequency = ScheduleFrequency.Monthly,
                    Cadence = "1st of each month, 08:00",
                    RecipientsCsv = "finance@giwu.ph,admin@giwu.ph",
                    Format = ReportFormat.Excel,
                    NextRunAt = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1),
                    LastRunAt = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc),
                    IsActive = true,
                },
                new ReportSchedule
                {
                    DefinitionId = "RD-CUSTOM-003", DefinitionName = "Attendance Exception Report",
                    Category = ReportCategory.Attendance,
                    Frequency = ScheduleFrequency.Weekly,
                    Cadence = "Monday 07:00",
                    RecipientsCsv = "hr@giwu.ph",
                    Format = ReportFormat.Csv,
                    NextRunAt = nowUtc.Date.AddDays(((int)DayOfWeek.Monday - (int)nowUtc.DayOfWeek + 7) % 7).AddHours(7),
                    LastRunAt = nowUtc.AddDays(-7),
                    IsActive = true,
                });

            // A few historical runs.
            db.ReportRuns.AddRange(
                new ReportRun
                {
                    DefinitionId = "RD-CUSTOM-002", DefinitionName = "Monthly Payroll Summary",
                    Category = ReportCategory.Payroll, Format = ReportFormat.Excel,
                    QueuedAt = nowUtc.AddDays(-8), CompletedAt = nowUtc.AddDays(-8).AddSeconds(14),
                    Status = ReportRunStatus.Completed, RanByDisplay = "Admin",
                    PeriodStart = today.AddMonths(-1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                    PeriodEnd = today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                    RowCount = 12, FileSizeBytes = 32_412,
                    FileName = "payroll-summary.xlsx",
                },
                new ReportRun
                {
                    DefinitionId = "RD-CUSTOM-003", DefinitionName = "Attendance Exception Report",
                    Category = ReportCategory.Attendance, Format = ReportFormat.Csv,
                    QueuedAt = nowUtc.AddDays(-3), CompletedAt = nowUtc.AddDays(-3).AddSeconds(6),
                    Status = ReportRunStatus.Completed, RanByDisplay = "Admin",
                    PeriodStart = today.AddDays(-7).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                    PeriodEnd = today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                    RowCount = 23, FileSizeBytes = 4_201,
                    FileName = "attendance-exceptions.csv",
                },
                new ReportRun
                {
                    DefinitionId = "RD-CUSTOM-001", DefinitionName = "Headcount by Department",
                    Category = ReportCategory.People, Format = ReportFormat.Pdf,
                    QueuedAt = nowUtc.AddDays(-1), CompletedAt = nowUtc.AddDays(-1).AddSeconds(3),
                    Status = ReportRunStatus.Completed, RanByDisplay = "Admin",
                    RowCount = 3, FileSizeBytes = 18_240,
                    FileName = "headcount-by-dept.pdf",
                },
                new ReportRun
                {
                    DefinitionId = "RD-CUSTOM-006", DefinitionName = "Leave Balance Snapshot",
                    Category = ReportCategory.Leave, Format = ReportFormat.Csv,
                    QueuedAt = nowUtc.AddHours(-2),
                    Status = ReportRunStatus.Failed, RanByDisplay = "Admin",
                    ErrorMessage = "Department filter is required for this report.",
                });
        }

        if (!hasDeadlines)
        {
            var year = today.Year;
            // Use day numbers that exist in every month (≤28) to avoid Feb edge cases.
            db.ComplianceDeadlines.AddRange(
                new ComplianceDeadline
                {
                    Agency = "BIR", FormCode = "1601-C",
                    Name = "Monthly Remittance of Income Taxes Withheld on Compensation",
                    Description = "Monthly e-FPS filing — due on the 10th of the following month.",
                    DueDate = new DateOnly(year, today.Month, 10),
                    PeriodCovered = $"{today.AddMonths(-1):yyyy-MM}",
                    IsFiled = false, RelatedReportCode = "RD-CMP-001",
                },
                new ComplianceDeadline
                {
                    Agency = "BIR", FormCode = "2316",
                    Name = "Certificate of Compensation Payment / Tax Withheld",
                    Description = "Annual employee tax certificate — due January 31.",
                    DueDate = new DateOnly(year, 1, 28),
                    PeriodCovered = $"{year - 1}",
                    IsFiled = true, FiledOn = new DateOnly(year, 1, 27),
                    RelatedReportCode = "RD-CMP-002",
                },
                new ComplianceDeadline
                {
                    Agency = "SSS", FormCode = "R-5",
                    Name = "SSS Monthly Contributions",
                    Description = "Employer share + employee withhold remittance.",
                    DueDate = new DateOnly(year, today.Month, 25),
                    PeriodCovered = $"{today.AddMonths(-1):yyyy-MM}",
                    IsFiled = false, RelatedReportCode = "RD-CMP-003",
                },
                new ComplianceDeadline
                {
                    Agency = "PhilHealth", FormCode = "RF-1",
                    Name = "Employer's Remittance Report",
                    Description = "Monthly PhilHealth premium remittance.",
                    DueDate = new DateOnly(year, today.Month, 15),
                    PeriodCovered = $"{today.AddMonths(-1):yyyy-MM}",
                    IsFiled = true, FiledOn = today.AddDays(-3),
                    RelatedReportCode = "RD-CMP-004",
                },
                new ComplianceDeadline
                {
                    Agency = "Pag-IBIG", FormCode = "MCRF",
                    Name = "HDMF Monthly Contributions",
                    Description = "Pag-IBIG monthly remittance.",
                    DueDate = new DateOnly(year, today.Month, 20),
                    PeriodCovered = $"{today.AddMonths(-1):yyyy-MM}",
                    IsFiled = false, RelatedReportCode = "RD-CMP-005",
                },
                new ComplianceDeadline
                {
                    Agency = "BIR", FormCode = "1604-CF",
                    Name = "Annual Info Return — Income Tax Withheld",
                    Description = "Annual reconciliation — due January 31.",
                    DueDate = new DateOnly(year, 1, 28),
                    PeriodCovered = $"{year - 1}",
                    IsFiled = true, FiledOn = new DateOnly(year, 1, 25),
                    RelatedReportCode = "RD-CMP-002",
                },
                new ComplianceDeadline
                {
                    Agency = "DOLE", FormCode = "RKS-5",
                    Name = "Annual Establishment Report",
                    Description = "DOLE annual employment data report.",
                    DueDate = new DateOnly(year, 1, 28),
                    PeriodCovered = $"{year - 1}",
                    IsFiled = true, FiledOn = new DateOnly(year, 1, 22),
                    RelatedReportCode = "RD-CMP-006",
                },
                new ComplianceDeadline
                {
                    Agency = "BIR", FormCode = "0619-E",
                    Name = "Monthly Remittance Form for Expanded Withholding",
                    Description = "Expanded withholding tax monthly remittance.",
                    DueDate = new DateOnly(year, today.Month, 10),
                    PeriodCovered = $"{today.AddMonths(-1):yyyy-MM}",
                    IsFiled = false, RelatedReportCode = "RD-PAY-002",
                },
                new ComplianceDeadline
                {
                    Agency = "SSS", FormCode = "—",
                    Name = "Maternity Notification Submissions",
                    Description = "Submit maternity notifications for the quarter.",
                    DueDate = today.AddDays(45),
                    PeriodCovered = $"Q{((today.Month - 1) / 3) + 1} {year}",
                    IsFiled = false, RelatedReportCode = "RD-CMP-003",
                },
                new ComplianceDeadline
                {
                    Agency = "BIR", FormCode = "1601-EQ",
                    Name = "Quarterly Remittance Return of EWT",
                    Description = "Quarterly EWT return — due end of month after the quarter.",
                    DueDate = today.AddDays(20),
                    PeriodCovered = $"Q{Math.Max(1, ((today.Month - 1) / 3))} {year}",
                    IsFiled = false, RelatedReportCode = "RD-PAY-002",
                });
        }

        await db.SaveChangesAsync(ct);
    }

    // ─── Helpers ────────────────────────────────────────────────────────────

    private static async Task<Dictionary<string, Guid>> EnsureDepartmentsAsync(
        ApplicationDbContext db, CancellationToken ct)
    {
        var wanted = new (string Name, string Code)[]
        {
            ("Human Resources", "HR"),
            ("Engineering",     "ENG"),
            ("Finance",         "FIN"),
        };

        foreach (var (name, code) in wanted)
        {
            if (!await db.Departments.AnyAsync(d => d.Code == code, ct))
                db.Departments.Add(new Department { Name = name, Code = code });
        }
        await db.SaveChangesAsync(ct);

        return await db.Departments
            .Where(d => wanted.Select(w => w.Code).Contains(d.Code))
            .ToDictionaryAsync(d => d.Code, d => d.Id, ct);
    }

    private static async Task<List<LeaveType>> EnsureLeaveTypesAsync(
        ApplicationDbContext db, CancellationToken ct)
    {
        var wanted = new (string Code, string Name, LeaveCategory Cat, decimal Days, LeaveAccrualMode Acc, bool Mandated, string? LegalRef)[]
        {
            ("VL",  "Vacation Leave",            LeaveCategory.Vacation, 15, LeaveAccrualMode.Yearly, false, null),
            ("SL",  "Sick Leave",                LeaveCategory.Sick,     15, LeaveAccrualMode.Yearly, false, null),
            ("SIL", "Service Incentive Leave",   LeaveCategory.Sil,       5, LeaveAccrualMode.Tenure, true,  "Labor Code Art. 95"),
        };

        foreach (var (code, name, cat, days, acc, mandated, legal) in wanted)
        {
            if (!await db.LeaveTypes.AnyAsync(t => t.Code == code, ct))
            {
                db.LeaveTypes.Add(new LeaveType
                {
                    Code = code, Name = name,
                    Category = cat,
                    AnnualEntitlementDays = days,
                    AccrualMode = acc,
                    IsMandated = mandated,
                    LegalReference = legal ?? "",
                });
            }
        }
        await db.SaveChangesAsync(ct);

        return await db.LeaveTypes.Where(t => t.IsActive).ToListAsync(ct);
    }

    private static async Task EnsureDemoUserAsync(
        ApplicationDbContext db, IPasswordHasher hasher,
        string email, string password, string roleName, Guid employeeId, string displayName,
        CancellationToken ct)
    {
        if (await db.Users.AnyAsync(u => u.Email == email, ct)) return;

        var role = await db.Roles.FirstOrDefaultAsync(r => r.Name == roleName, ct);
        if (role is null) return; // bootstrap seeder should have created it

        var user = new User
        {
            Email        = email,
            PasswordHash = hasher.Hash(password),
            DisplayName  = displayName,
            EmployeeId   = employeeId,
            IsActive     = true,
        };
        user.Roles.Add(new UserRoleAssignment { RoleId = role.Id });
        db.Users.Add(user);
    }
}
