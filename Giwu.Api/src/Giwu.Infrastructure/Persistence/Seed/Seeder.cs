using Giwu.Application.Common;
using Giwu.Domain.Attendance;
using Giwu.Domain.Benefits;
using Giwu.Domain.Common;
using Giwu.Domain.Identity;
using Giwu.Domain.Leaves;
using Giwu.Domain.Organization;
using Giwu.Domain.Payroll;
using Giwu.Domain.Recruitment;
using Giwu.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using TenancyPayFrequency = Giwu.Domain.Tenancy.PayFrequency;
using OrgPayFrequency = Giwu.Domain.Organization.PayFrequency;

namespace Giwu.Infrastructure.Persistence.Seed;

/// <summary>
/// Idempotent seeding for built-in roles, default permissions, a demo tenant
/// and admin user. Safe to run on every startup.
/// </summary>
public static class Seeder
{
    public static async Task SeedAsync(
        ApplicationDbContext db,
        IPasswordHasher hasher,
        ITenantContext tenant,
        CancellationToken ct = default)
    {
        tenant.Bypass();

        // ── Demo tenant ─────────────────────────────────────────────────────
        var demo = await db.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Name == "Giwu Demo", ct);
        if (demo is null)
        {
            demo = new Tenant
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Name = "Giwu Demo",
            };
            db.Tenants.Add(demo);
        }

        // Populate rich demo values, but only into fields the operator hasn't edited.
        // This keeps the seed idempotent for fresh DBs and safe-to-rerun on existing
        // DBs without clobbering manual edits via the Settings page.
        EnrichDemoTenant(demo);
        await db.SaveChangesAsync(ct);

        tenant.SetTenant(demo.Id);

        // ── System roles + default permission map ───────────────────────────
        var defaults = new Dictionary<string, string[]>
        {
            [SystemRoles.HrAdmin]      = Permissions.All.ToArray(),
            [SystemRoles.HrSpecialist] = new[]
            {
                Permissions.Employees.View, Permissions.Employees.Manage,
                Permissions.Departments.View, Permissions.Departments.Manage,
                Permissions.Attendance.ViewAll,
                Permissions.Leaves.ViewAll, Permissions.Leaves.Approve, Permissions.Leaves.Manage,
                Permissions.Recruitment.View, Permissions.Recruitment.Manage,
                Permissions.Benefits.View,
                Permissions.Reports.View, Permissions.Reports.Run,
                Permissions.Settings.View,
            },
            [SystemRoles.Manager] = new[]
            {
                Permissions.Employees.View,
                Permissions.Attendance.ViewAll,
                Permissions.Leaves.ViewAll, Permissions.Leaves.Approve,
                Permissions.Reports.View,
            },
            [SystemRoles.Finance] = new[]
            {
                Permissions.Payroll.Run, Permissions.Payroll.Approve, Permissions.Payroll.ViewAll,
                Permissions.Benefits.View, Permissions.Benefits.Manage,
                Permissions.Reports.View, Permissions.Reports.Run,
            },
            [SystemRoles.Employee] = new[]
            {
                Permissions.Attendance.ViewSelf,
                Permissions.Leaves.FileSelf,
            },
        };

        foreach (var (name, perms) in defaults)
        {
            var role = await db.Roles.FirstOrDefaultAsync(r => r.Name == name, ct);
            if (role is null)
            {
                role = new Role { Name = name, IsSystem = true, TenantId = demo.Id };
                db.Roles.Add(role);
                await db.SaveChangesAsync(ct);
            }

            var existing = await db.RolePermissions
                .Where(p => p.RoleId == role.Id).Select(p => p.PermissionKey).ToListAsync(ct);
            foreach (var p in perms.Except(existing))
                db.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionKey = p });
        }
        await db.SaveChangesAsync(ct);

        // ── Demo HR Admin user ──────────────────────────────────────────────
        var admin = await db.Users.FirstOrDefaultAsync(u => u.Email == "admin@giwu.ph", ct);
        if (admin is null)
        {
            var hrAdminRole = await db.Roles.FirstAsync(r => r.Name == SystemRoles.HrAdmin, ct);
            admin = new User
            {
                Email        = "admin@giwu.ph",
                PasswordHash = hasher.Hash("ChangeMe!123"),
                DisplayName  = "Demo Admin",
                IsActive     = true,
                TenantId     = demo.Id,
            };
            admin.Roles.Add(new UserRoleAssignment { RoleId = hrAdminRole.Id });
            db.Users.Add(admin);
            await db.SaveChangesAsync(ct);
        }

        // ── Demo department + leave types ───────────────────────────────────
        if (!await db.Departments.AnyAsync(ct))
        {
            db.Departments.Add(new Department { Name = "Human Resources", Code = "HR" });
            db.Departments.Add(new Department { Name = "Engineering",     Code = "ENG" });
            db.Departments.Add(new Department { Name = "Finance",         Code = "FIN" });
            await db.SaveChangesAsync(ct);
        }

        if (!await db.LeaveTypes.AnyAsync(ct))
        {
            db.LeaveTypes.Add(new LeaveType { Name = "Vacation Leave", Code = "VL", Category = LeaveCategory.Vacation,    AnnualEntitlementDays = 15, AccrualMode = LeaveAccrualMode.Yearly });
            db.LeaveTypes.Add(new LeaveType { Name = "Sick Leave",     Code = "SL", Category = LeaveCategory.Sick,        AnnualEntitlementDays = 15, AccrualMode = LeaveAccrualMode.Yearly });
            db.LeaveTypes.Add(new LeaveType { Name = "Service Incentive Leave", Code = "SIL", Category = LeaveCategory.Sil, IsMandated = true, AnnualEntitlementDays = 5, AccrualMode = LeaveAccrualMode.Tenure, LegalReference = "Labor Code Art. 95" });
            await db.SaveChangesAsync(ct);
        }

        await SeedDemoDataAsync(db, ct);
    }

    // ── Rich demo dataset (employees, attendance, leaves, payroll, recruitment, benefits) ─
    // Only seeds when Employees is empty, so it's safe to re-run on existing DBs.
    private static async Task SeedDemoDataAsync(ApplicationDbContext db, CancellationToken ct)
    {
        if (await db.Employees.AnyAsync(ct)) return;

        var rng = new Random(20260507);
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);

        var hr  = await db.Departments.FirstAsync(d => d.Code == "HR",  ct);
        var eng = await db.Departments.FirstAsync(d => d.Code == "ENG", ct);
        var fin = await db.Departments.FirstAsync(d => d.Code == "FIN", ct);

        // ── Standard shift + employees ──────────────────────────────────────
        var shift = new Shift
        {
            Name = "Standard Day",
            StartTime    = new TimeOnly(8, 0),
            EndTime      = new TimeOnly(17, 0),
            BreakMinutes = 60,
            WorkDays     = new[] { true, true, true, true, true, false, false }, // Mon–Fri
            GraceMinutes = 10,
        };
        db.Shifts.Add(shift);

        var seed = new (string First, string Last, string Title, Guid DeptId, decimal Salary, int TenureYears, bool IsManager)[]
        {
            ("Maria",   "Santos",     "HR Director",            hr.Id,  120_000m, 8, true),
            ("Joshua",  "Reyes",      "HR Specialist",          hr.Id,   55_000m, 3, false),
            ("Liza",    "Cruz",       "Recruiter",              hr.Id,   48_000m, 2, false),
            ("Daniel",  "Aquino",     "Engineering Manager",    eng.Id, 150_000m, 6, true),
            ("Patricia","Dela Cruz",  "Senior Software Engineer", eng.Id, 95_000m, 5, false),
            ("Mark",    "Tolentino",  "Software Engineer",      eng.Id,  70_000m, 2, false),
            ("Ana",     "Villanueva", "Software Engineer",      eng.Id,  68_000m, 1, false),
            ("Kevin",   "Bautista",   "QA Engineer",            eng.Id,  60_000m, 1, false),
            ("Rosa",    "Mendoza",    "Finance Director",       fin.Id, 130_000m, 7, true),
            ("Carlo",   "Lim",        "Senior Accountant",      fin.Id,  72_000m, 4, false),
            ("Bea",     "Gonzales",   "Payroll Specialist",     fin.Id,  55_000m, 2, false),
            ("Mike",    "Garcia",     "Bookkeeper",             fin.Id,  42_000m, 1, false),
        };

        var employees = new List<Employee>();
        var managerByDept = new Dictionary<Guid, Guid>();

        foreach (var (first, last, title, deptId, salary, tenure, isManager) in seed)
        {
            var emp = new Employee
            {
                EmployeeNumber  = $"EMP-{1000 + employees.Count + 1:D4}",
                FirstName       = first,
                LastName        = last,
                Email           = $"{first.ToLowerInvariant()}.{last.Replace(" ", "").ToLowerInvariant()}@giwu.ph",
                PersonalEmail   = $"{first.ToLowerInvariant()}{last.Replace(" ", "").ToLowerInvariant()}@gmail.com",
                Phone           = $"+63 917 {rng.Next(100, 999)} {rng.Next(1000, 9999)}",
                BirthDate       = new DateOnly(1985 + rng.Next(0, 15), rng.Next(1, 13), rng.Next(1, 28)),
                Gender          = rng.Next(2) == 0 ? Gender.Male : Gender.Female,
                CivilStatus     = (CivilStatus)rng.Next(0, 3),
                DepartmentId    = deptId,
                JobTitle        = title,
                HireDate        = today.AddYears(-tenure).AddMonths(-rng.Next(0, 12)),
                RegularizationDate = today.AddYears(-tenure).AddMonths(-rng.Next(0, 12)).AddMonths(6),
                Status          = EmploymentStatus.Active,
                EmploymentType  = EmploymentType.Regular,
                WorkArrangement = (WorkArrangement)rng.Next(0, 3),
                MonthlyBaseSalary = salary,
                PayFrequency    = OrgPayFrequency.SemiMonthly,
                BankName        = "BPI",
                BankAccountNumber = $"1234-{rng.Next(1000, 9999)}-{rng.Next(10, 99)}",
                Tin             = $"{rng.Next(100, 999)}-{rng.Next(100, 999)}-{rng.Next(100, 999)}-000",
                SssNumber       = $"03-{rng.Next(1000000, 9999999)}-{rng.Next(0, 9)}",
                PhilHealthNumber= $"00-{rng.Next(100000000, 999999999)}-{rng.Next(0, 9)}",
                PagibigNumber   = $"{rng.Next(1000, 9999)}-{rng.Next(1000, 9999)}-{rng.Next(1000, 9999)}",
                PermanentAddress = new Address { Line1 = $"{rng.Next(1, 999)} Mabini St", City = "Quezon City", Province = "Metro Manila", PostalCode = "1100" },
                CurrentAddress   = new Address { Line1 = $"{rng.Next(1, 999)} Mabini St", City = "Quezon City", Province = "Metro Manila", PostalCode = "1100" },
                Emergency        = new EmergencyContact { Name = $"Emergency {last}", Relationship = "Spouse", Phone = $"+63 918 {rng.Next(100, 999)} {rng.Next(1000, 9999)}" },
            };
            db.Employees.Add(emp);
            employees.Add(emp);
            if (isManager) managerByDept[deptId] = emp.Id;
        }

        // Hook reports to their dept manager.
        foreach (var emp in employees)
        {
            if (managerByDept.TryGetValue(emp.DepartmentId, out var mgrId) && mgrId != emp.Id)
                emp.ManagerId = mgrId;
        }

        // Set department heads.
        hr.HeadEmployeeId  = managerByDept[hr.Id];
        eng.HeadEmployeeId = managerByDept[eng.Id];
        fin.HeadEmployeeId = managerByDept[fin.Id];

        // Assign all employees to the standard shift.
        foreach (var emp in employees)
        {
            db.EmployeeShiftAssignments.Add(new EmployeeShiftAssignment
            {
                EmployeeId = emp.Id,
                ShiftId    = shift.Id,
                EffectiveFrom = emp.HireDate,
            });
        }

        await db.SaveChangesAsync(ct);

        // ── Attendance: last 30 days, weekdays only, mostly Present ─────────
        for (int dayOffset = 30; dayOffset >= 1; dayOffset--)
        {
            var date = today.AddDays(-dayOffset);
            if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday) continue;

            foreach (var emp in employees)
            {
                var roll = rng.Next(0, 100);
                AttendanceStatus status;
                int? lateMin = null;
                DateTimeOffset? clockIn  = null;
                DateTimeOffset? clockOut = null;

                if (roll < 80)         { status = AttendanceStatus.Present; }
                else if (roll < 92)    { status = AttendanceStatus.Late; lateMin = rng.Next(5, 45); }
                else if (roll < 96)    { status = AttendanceStatus.Absent; }
                else                   { status = AttendanceStatus.OnLeave; }

                if (status is AttendanceStatus.Present or AttendanceStatus.Late)
                {
                    var inMinutes  = lateMin ?? rng.Next(-15, 5);
                    clockIn  = new DateTimeOffset(date.ToDateTime(new TimeOnly(8, 0)).AddMinutes(inMinutes), TimeSpan.FromHours(8));
                    clockOut = new DateTimeOffset(date.ToDateTime(new TimeOnly(17, 0)).AddMinutes(rng.Next(0, 30)), TimeSpan.FromHours(8));
                }

                db.AttendanceRecords.Add(new AttendanceRecord
                {
                    EmployeeId      = emp.Id,
                    Date            = date,
                    ClockIn         = clockIn,
                    ClockOut        = clockOut,
                    BreakMinutes    = clockIn.HasValue ? 60 : null,
                    ClockInSource   = AttendanceSource.Biometric,
                    ClockOutSource  = AttendanceSource.Biometric,
                    Status          = status,
                    LateMinutes     = lateMin,
                });
            }
        }

        // ── Leave balances + a handful of requests ──────────────────────────
        var leaveTypes = await db.LeaveTypes.ToListAsync(ct);
        var year = today.Year;

        foreach (var emp in employees)
        {
            foreach (var lt in leaveTypes)
            {
                db.LeaveBalances.Add(new LeaveBalance
                {
                    EmployeeId   = emp.Id,
                    LeaveTypeId  = lt.Id,
                    PeriodYear   = year,
                    Entitlement  = lt.AnnualEntitlementDays,
                    CarryOver    = 0,
                    Used         = rng.Next(0, (int)Math.Max(1, lt.AnnualEntitlementDays / 2)),
                    Pending      = 0,
                });
            }
        }

        var vl = leaveTypes.First(t => t.Code == "VL");
        var sl = leaveTypes.First(t => t.Code == "SL");
        var hrAdmin = managerByDept[hr.Id];

        // 3 pending, 2 approved, 1 rejected — across different employees.
        var leaveRequests = new[]
        {
            (employees[1].Id, vl.Id, today.AddDays(7),  today.AddDays(9),  3m, "Family event",            LeaveRequestStatus.Pending,  (Guid?)null,    (string)""),
            (employees[5].Id, sl.Id, today.AddDays(2),  today.AddDays(2),  1m, "Doctor appointment",       LeaveRequestStatus.Pending,  (Guid?)null,    ""),
            (employees[7].Id, vl.Id, today.AddDays(14), today.AddDays(18), 5m, "Vacation in Boracay",      LeaveRequestStatus.Pending,  (Guid?)null,    ""),
            (employees[2].Id, sl.Id, today.AddDays(-5), today.AddDays(-4), 2m, "Flu",                       LeaveRequestStatus.Approved, hrAdmin,        "Approved with med cert"),
            (employees[10].Id, vl.Id, today.AddDays(-12), today.AddDays(-10), 3m, "Personal",               LeaveRequestStatus.Approved, hrAdmin,        "Approved"),
            (employees[6].Id, vl.Id, today.AddDays(3),  today.AddDays(5),  3m, "Out of country trip",       LeaveRequestStatus.Rejected, hrAdmin,        "Conflicts with sprint launch"),
        };
        foreach (var (empId, ltId, start, end, days, reason, status, resolvedBy, note) in leaveRequests)
        {
            db.LeaveRequests.Add(new LeaveRequest
            {
                EmployeeId    = empId,
                LeaveTypeId   = ltId,
                StartDate     = start,
                EndDate       = end,
                DaysRequested = days,
                Reason        = reason,
                Status        = status,
                ResolvedById  = resolvedBy,
                ResolvedAt    = resolvedBy.HasValue ? DateTimeOffset.UtcNow.AddDays(-2) : null,
                ResolutionNote = note,
            });
        }

        // ── Pay periods + payslips ──────────────────────────────────────────
        // 2 fully-released past periods, 1 current Calculated.
        var periods = new[]
        {
            new PayPeriod
            {
                Code         = $"{today.AddMonths(-2):yyyy-MM}-2H",
                PeriodStart  = new DateOnly(today.AddMonths(-2).Year, today.AddMonths(-2).Month, 16),
                PeriodEnd    = new DateOnly(today.AddMonths(-2).Year, today.AddMonths(-2).Month, DateTime.DaysInMonth(today.AddMonths(-2).Year, today.AddMonths(-2).Month)),
                ReleaseDate  = new DateOnly(today.AddMonths(-1).Year, today.AddMonths(-1).Month, 5),
                Frequency    = "SemiMonthly",
                Status       = PayPeriodStatus.Released,
                ApprovedById = hrAdmin,
                ApprovedAt   = DateTimeOffset.UtcNow.AddMonths(-1),
            },
            new PayPeriod
            {
                Code         = $"{today.AddMonths(-1):yyyy-MM}-1H",
                PeriodStart  = new DateOnly(today.AddMonths(-1).Year, today.AddMonths(-1).Month, 1),
                PeriodEnd    = new DateOnly(today.AddMonths(-1).Year, today.AddMonths(-1).Month, 15),
                ReleaseDate  = new DateOnly(today.AddMonths(-1).Year, today.AddMonths(-1).Month, 20),
                Frequency    = "SemiMonthly",
                Status       = PayPeriodStatus.Released,
                ApprovedById = hrAdmin,
                ApprovedAt   = DateTimeOffset.UtcNow.AddDays(-20),
            },
            new PayPeriod
            {
                Code         = $"{today:yyyy-MM}-1H",
                PeriodStart  = new DateOnly(today.Year, today.Month, 1),
                PeriodEnd    = new DateOnly(today.Year, today.Month, 15),
                Frequency    = "SemiMonthly",
                Status       = PayPeriodStatus.Calculated,
            },
        };
        db.PayPeriods.AddRange(periods);
        await db.SaveChangesAsync(ct);

        foreach (var period in periods)
        {
            var releasedStatus = period.Status == PayPeriodStatus.Released ? PayslipStatus.Released : PayslipStatus.Pending;
            foreach (var emp in employees)
            {
                var halfMonthSalary = emp.MonthlyBaseSalary / 2m;
                var sss        = Math.Round(halfMonthSalary * 0.045m, 2);
                var philHealth = Math.Round(halfMonthSalary * 0.025m, 2);
                var pagIbig    = 100m;
                var tax        = Math.Round((halfMonthSalary - sss - philHealth - pagIbig) * 0.10m, 2);
                var ot         = period.Status == PayPeriodStatus.Released ? rng.Next(0, 3000) : 0m;

                db.Payslips.Add(new Payslip
                {
                    PayPeriodId    = period.Id,
                    EmployeeId     = emp.Id,
                    BasicSalary    = halfMonthSalary,
                    Overtime       = ot,
                    Sss            = sss,
                    PhilHealth     = philHealth,
                    PagIbig        = pagIbig,
                    WithholdingTax = tax,
                    Status         = releasedStatus,
                });
            }
        }

        // ── Recruitment: 3 open reqs, 8 candidates, 3 interviews ────────────
        var reqs = new[]
        {
            new JobRequisition
            {
                Title          = "Senior Backend Engineer",
                DepartmentId   = eng.Id,
                Location       = "Makati / Hybrid",
                EmploymentType = "Full-time",
                Openings       = 2,
                Filled         = 0,
                Status         = JobStatus.Open,
                OwnerEmployeeId = managerByDept[eng.Id],
                SalaryMin      = 90_000m,
                SalaryMax      = 140_000m,
                PostedAt       = DateTimeOffset.UtcNow.AddDays(-21),
                TargetFillBy   = today.AddDays(45),
                Description    = "Build payroll and attendance services on .NET 10 and Postgres.",
            },
            new JobRequisition
            {
                Title          = "Talent Acquisition Specialist",
                DepartmentId   = hr.Id,
                Location       = "Makati / Onsite",
                EmploymentType = "Full-time",
                Openings       = 1,
                Filled         = 0,
                Status         = JobStatus.Open,
                OwnerEmployeeId = managerByDept[hr.Id],
                SalaryMin      = 50_000m,
                SalaryMax      = 75_000m,
                PostedAt       = DateTimeOffset.UtcNow.AddDays(-12),
                TargetFillBy   = today.AddDays(30),
                Description    = "End-to-end recruitment for Engineering and Finance hires.",
            },
            new JobRequisition
            {
                Title          = "Junior Accountant",
                DepartmentId   = fin.Id,
                Location       = "Makati / Onsite",
                EmploymentType = "Full-time",
                Openings       = 1,
                Filled         = 1,
                Status         = JobStatus.Closed,
                OwnerEmployeeId = managerByDept[fin.Id],
                SalaryMin      = 35_000m,
                SalaryMax      = 50_000m,
                PostedAt       = DateTimeOffset.UtcNow.AddDays(-90),
                TargetFillBy   = today.AddDays(-30),
                Description    = "Day-to-day bookkeeping and BIR filings.",
            },
        };
        db.JobRequisitions.AddRange(reqs);
        await db.SaveChangesAsync(ct);

        var backendReq = reqs[0];
        var taReq      = reqs[1];
        var accountantReq = reqs[2];

        var candidates = new[]
        {
            new Candidate { JobRequisitionId = backendReq.Id, FirstName = "Jasmine",  LastName = "Lopez",    Email = "jasmine.lopez@example.com",  Phone = "+63 917 100 1001", Stage = CandidateStage.Applied,    Source = "LinkedIn",      Rating = 0, AppliedAt = DateTimeOffset.UtcNow.AddDays(-3),  LastActivityAt = DateTimeOffset.UtcNow.AddDays(-3) },
            new Candidate { JobRequisitionId = backendReq.Id, FirstName = "Renz",     LastName = "Pascual",  Email = "renz.pascual@example.com",   Phone = "+63 917 100 1002", Stage = CandidateStage.Screening,  Source = "Indeed",        Rating = 3, AppliedAt = DateTimeOffset.UtcNow.AddDays(-7),  LastActivityAt = DateTimeOffset.UtcNow.AddDays(-2) },
            new Candidate { JobRequisitionId = backendReq.Id, FirstName = "Ivy",      LastName = "Chua",     Email = "ivy.chua@example.com",       Phone = "+63 917 100 1003", Stage = CandidateStage.Interview,  Source = "Referral",      Rating = 4, AppliedAt = DateTimeOffset.UtcNow.AddDays(-12), LastActivityAt = DateTimeOffset.UtcNow.AddDays(-1) },
            new Candidate { JobRequisitionId = backendReq.Id, FirstName = "Lance",    LastName = "Domingo",  Email = "lance.domingo@example.com",  Phone = "+63 917 100 1004", Stage = CandidateStage.Offer,      Source = "JobStreet",     Rating = 5, AppliedAt = DateTimeOffset.UtcNow.AddDays(-18), LastActivityAt = DateTimeOffset.UtcNow.AddDays(-1) },
            new Candidate { JobRequisitionId = backendReq.Id, FirstName = "Karl",     LastName = "Sy",       Email = "karl.sy@example.com",        Phone = "+63 917 100 1005", Stage = CandidateStage.Rejected,   Source = "LinkedIn",      Rating = 2, AppliedAt = DateTimeOffset.UtcNow.AddDays(-14), LastActivityAt = DateTimeOffset.UtcNow.AddDays(-5), RejectionReason = "Below expected experience" },
            new Candidate { JobRequisitionId = taReq.Id,      FirstName = "Mika",     LastName = "Ramos",    Email = "mika.ramos@example.com",     Phone = "+63 917 100 2001", Stage = CandidateStage.Screening,  Source = "Referral",      Rating = 4, AppliedAt = DateTimeOffset.UtcNow.AddDays(-6),  LastActivityAt = DateTimeOffset.UtcNow.AddDays(-1) },
            new Candidate { JobRequisitionId = taReq.Id,      FirstName = "Paolo",    LastName = "Yap",      Email = "paolo.yap@example.com",      Phone = "+63 917 100 2002", Stage = CandidateStage.Interview,  Source = "Indeed",        Rating = 4, AppliedAt = DateTimeOffset.UtcNow.AddDays(-9),  LastActivityAt = DateTimeOffset.UtcNow.AddDays(-2) },
            new Candidate { JobRequisitionId = accountantReq.Id, FirstName = "Mike",  LastName = "Garcia",   Email = "mike.garcia@example.com",    Phone = "+63 917 100 3001", Stage = CandidateStage.Hired,      Source = "Referral",      Rating = 5, AppliedAt = DateTimeOffset.UtcNow.AddDays(-100),LastActivityAt = DateTimeOffset.UtcNow.AddDays(-25) },
        };
        db.Candidates.AddRange(candidates);
        await db.SaveChangesAsync(ct);

        var interviewer = managerByDept[eng.Id];
        db.Interviews.AddRange(
            new Interview { CandidateId = candidates[2].Id, ScheduledAt = DateTimeOffset.UtcNow.AddDays(2),  DurationMinutes = 60, Kind = InterviewKind.Technical,  InterviewerEmployeeId = interviewer, Location = "Zoom",      Status = InterviewStatus.Scheduled },
            new Interview { CandidateId = candidates[3].Id, ScheduledAt = DateTimeOffset.UtcNow.AddDays(-1), DurationMinutes = 90, Kind = InterviewKind.Final,      InterviewerEmployeeId = interviewer, Location = "Onsite",    Status = InterviewStatus.Completed, Notes = "Strong, send offer." },
            new Interview { CandidateId = candidates[6].Id, ScheduledAt = DateTimeOffset.UtcNow.AddDays(3),  DurationMinutes = 45, Kind = InterviewKind.PhoneScreen, InterviewerEmployeeId = managerByDept[hr.Id], Location = "Phone", Status = InterviewStatus.Scheduled }
        );

        // ── Benefit programs + enrollments ──────────────────────────────────
        var hmo = new BenefitProgram
        {
            Name = "HMO Coverage", Provider = "Maxicare", Category = BenefitCategory.Health,
            Description = "Comprehensive medical coverage with 200K MBL.",
            IsActive = true, IsMandatory = false, Eligibility = "Regular employees",
            EffectiveDate = today.AddYears(-2), RenewalDate = today.AddMonths(4),
            MonthlyCostPerEmployee = 1_800m, EmployerShare = 100m, EmployeeShare = 0m, ShareIsPercent = true,
        };
        var life = new BenefitProgram
        {
            Name = "Group Life Insurance", Provider = "Sun Life", Category = BenefitCategory.Insurance,
            Description = "1M coverage for accidental death and disability.",
            IsActive = true, IsMandatory = true, Eligibility = "All regular employees",
            EffectiveDate = today.AddYears(-3), RenewalDate = today.AddMonths(7),
            MonthlyCostPerEmployee = 250m, EmployerShare = 100m, EmployeeShare = 0m, ShareIsPercent = true,
        };
        var rice = new BenefitProgram
        {
            Name = "Rice Allowance", Provider = "Internal", Category = BenefitCategory.Allowance,
            Description = "Tax-exempt rice subsidy of PHP 2,000/month.",
            IsActive = true, IsMandatory = false, Eligibility = "All employees",
            EffectiveDate = today.AddYears(-5), MonthlyCostPerEmployee = 2_000m,
            EmployerShare = 100m, EmployeeShare = 0m, ShareIsPercent = true,
        };
        var sssLoan = new BenefitProgram
        {
            Name = "SSS Salary Loan", Provider = "SSS", Category = BenefitCategory.Loan,
            Description = "Government salary loan facility, repaid via payroll.",
            IsActive = true, IsMandatory = false, Eligibility = "Members with 36+ contributions",
            EffectiveDate = today.AddYears(-10), MonthlyCostPerEmployee = 0m,
        };
        var wellness = new BenefitProgram
        {
            Name = "Mental Wellness", Provider = "MindNation", Category = BenefitCategory.Wellness,
            Description = "Telehealth counseling sessions, 4 free sessions/year.",
            IsActive = true, IsMandatory = false, Eligibility = "Regular employees",
            EffectiveDate = today.AddMonths(-6), RenewalDate = today.AddMonths(6),
            MonthlyCostPerEmployee = 350m, EmployerShare = 100m, EmployeeShare = 0m, ShareIsPercent = true,
        };
        db.BenefitPrograms.AddRange(hmo, life, rice, sssLoan, wellness);
        await db.SaveChangesAsync(ct);

        // Everyone gets HMO + life + rice. Wellness for ENG only.
        foreach (var emp in employees)
        {
            db.BenefitEnrollments.Add(new BenefitEnrollment
            {
                EmployeeId = emp.Id, BenefitProgramId = hmo.Id,
                EnrolledOn = emp.HireDate, Status = EnrollmentStatus.Active,
                MonthlyContribution = 0m,
            });
            db.BenefitEnrollments.Add(new BenefitEnrollment
            {
                EmployeeId = emp.Id, BenefitProgramId = life.Id,
                EnrolledOn = emp.HireDate, Status = EnrollmentStatus.Active,
                MonthlyContribution = 0m,
            });
            db.BenefitEnrollments.Add(new BenefitEnrollment
            {
                EmployeeId = emp.Id, BenefitProgramId = rice.Id,
                EnrolledOn = emp.HireDate, Status = EnrollmentStatus.Active,
                MonthlyContribution = 0m,
            });
            if (emp.DepartmentId == eng.Id)
            {
                db.BenefitEnrollments.Add(new BenefitEnrollment
                {
                    EmployeeId = emp.Id, BenefitProgramId = wellness.Id,
                    EnrolledOn = today.AddMonths(-3), Status = EnrollmentStatus.Active,
                    MonthlyContribution = 0m,
                });
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private static void EnrichDemoTenant(Tenant t)
    {
        // Only fill blanks — never overwrite values an operator may have edited
        // via the Settings page.
        if (string.IsNullOrWhiteSpace(t.LegalName))                t.LegalName                = "Giwu HR Solutions, Inc.";
        if (string.IsNullOrWhiteSpace(t.TradeName))                t.TradeName                = "Giwu HR";
        if (string.IsNullOrWhiteSpace(t.Address))                  t.Address                  = "23F BPI-Philam Tower, 6811 Ayala Ave";
        if (string.IsNullOrWhiteSpace(t.City))                     t.City                     = "Makati City";
        if (string.IsNullOrWhiteSpace(t.Province))                 t.Province                 = "Metro Manila";
        if (string.IsNullOrWhiteSpace(t.PostalCode))               t.PostalCode               = "1226";
        if (string.IsNullOrWhiteSpace(t.Phone))                    t.Phone                    = "+63 2 8888 1234";
        if (string.IsNullOrWhiteSpace(t.Email))                    t.Email                    = "info@giwu.ph";
        if (string.IsNullOrWhiteSpace(t.Website))                  t.Website                  = "https://giwu.ph";
        if (string.IsNullOrWhiteSpace(t.Tin))                      t.Tin                      = "001-234-567-000";
        if (string.IsNullOrWhiteSpace(t.RdoCode))                  t.RdoCode                  = "044";
        if (string.IsNullOrWhiteSpace(t.SssEmployerNumber))        t.SssEmployerNumber        = "03-9876543-2";
        if (string.IsNullOrWhiteSpace(t.PhilHealthEmployerNumber)) t.PhilHealthEmployerNumber = "00-000123456-7";
        if (string.IsNullOrWhiteSpace(t.PagibigEmployerNumber))    t.PagibigEmployerNumber    = "1234-5678-9012";
        if (string.IsNullOrWhiteSpace(t.DoleEstablishmentNumber))  t.DoleEstablishmentNumber  = "DOLE-NCR-2018-0001";

        if (string.IsNullOrWhiteSpace(t.Branding.CompanyName))     t.Branding.CompanyName     = "Giwu HR";
        if (string.IsNullOrWhiteSpace(t.Branding.AccentColor))     t.Branding.AccentColor     = "#1D9E75";
        // LogoDataUrl intentionally left blank — operator uploads via Settings.

        if (string.IsNullOrWhiteSpace(t.Localization.Timezone))       t.Localization.Timezone       = "Asia/Manila";
        if (string.IsNullOrWhiteSpace(t.Localization.CurrencyCode))   t.Localization.CurrencyCode   = "PHP";
        if (string.IsNullOrWhiteSpace(t.Localization.CurrencySymbol)) t.Localization.CurrencySymbol = "₱";
        // Other Localization fields are enums/ints with sensible defaults.
    }
}
