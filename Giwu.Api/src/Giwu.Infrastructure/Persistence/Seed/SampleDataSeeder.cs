using Giwu.Application.Common;
using Giwu.Domain.Attendance;
using Giwu.Domain.Common;
using Giwu.Domain.Identity;
using Giwu.Domain.Leaves;
using Giwu.Domain.Organization;
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

        // ── A couple of demo user accounts for the non-admin roles ────────
        await EnsureDemoUserAsync(db, hasher, "manager@manna-hris.example", "ChangeMe!123",
            SystemRoles.Manager, employees.First(e => e.LastName == "Aquino").Id, "Demo Manager", ct);
        await EnsureDemoUserAsync(db, hasher, "employee@manna-hris.example", "ChangeMe!123",
            SystemRoles.Employee, employees.First(e => e.LastName == "Tolentino").Id, "Demo Employee", ct);

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
