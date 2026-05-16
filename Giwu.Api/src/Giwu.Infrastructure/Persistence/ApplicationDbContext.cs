using Giwu.Application.Common;
using Giwu.Domain.Attendance;
using Giwu.Domain.Audit;
using Giwu.Domain.Benefits;
using Giwu.Domain.Common;
using Giwu.Domain.Identity;
using Giwu.Domain.Leaves;
using Giwu.Domain.Notifications;
using Giwu.Domain.Organization;
using Giwu.Domain.Outbox;
using Giwu.Domain.Payroll;
using Giwu.Domain.Recruitment;
using Giwu.Domain.Reports;
using Giwu.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Giwu.Infrastructure.Persistence;

public sealed class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    ITenantContext tenant,
    ICurrentUser user,
    TimeProvider clock)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<Tenant>          Tenants          => Set<Tenant>();
    public DbSet<User>            Users            => Set<User>();
    public DbSet<Role>            Roles            => Set<Role>();
    public DbSet<RolePermission>  RolePermissions  => Set<RolePermission>();
    public DbSet<RefreshToken>    RefreshTokens    => Set<RefreshToken>();
    public DbSet<Department>      Departments      => Set<Department>();
    public DbSet<Employee>        Employees        => Set<Employee>();
    public DbSet<LeaveType>       LeaveTypes       => Set<LeaveType>();
    public DbSet<LeaveRequest>    LeaveRequests    => Set<LeaveRequest>();
    public DbSet<LeaveBalance>    LeaveBalances    => Set<LeaveBalance>();
    public DbSet<Shift>                    Shifts                   => Set<Shift>();
    public DbSet<EmployeeShiftAssignment>  EmployeeShiftAssignments => Set<EmployeeShiftAssignment>();
    public DbSet<AttendanceRecord>         AttendanceRecords        => Set<AttendanceRecord>();
    public DbSet<OvertimeRequest>          OvertimeRequests         => Set<OvertimeRequest>();
    public DbSet<OutboxMessage>   Outbox           => Set<OutboxMessage>();
    public DbSet<AuditEvent>      AuditEvents      => Set<AuditEvent>();

    public DbSet<JobRequisition>    JobRequisitions    => Set<JobRequisition>();
    public DbSet<Candidate>         Candidates         => Set<Candidate>();
    public DbSet<Interview>         Interviews         => Set<Interview>();

    public DbSet<PayPeriod>         PayPeriods         => Set<PayPeriod>();
    public DbSet<Payslip>           Payslips           => Set<Payslip>();

    public DbSet<BenefitProgram>    BenefitPrograms    => Set<BenefitProgram>();
    public DbSet<BenefitEnrollment> BenefitEnrollments => Set<BenefitEnrollment>();
    public DbSet<BenefitRequest>    BenefitRequests    => Set<BenefitRequest>();

    public DbSet<ReportDefinition>     ReportDefinitions   => Set<ReportDefinition>();
    public DbSet<ReportSchedule>       ReportSchedules     => Set<ReportSchedule>();
    public DbSet<ReportRun>            ReportRuns          => Set<ReportRun>();
    public DbSet<ComplianceDeadline>   ComplianceDeadlines => Set<ComplianceDeadline>();

    public DbSet<Notification>         Notifications       => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // ── Tenant (no tenant filter on the tenant table itself) ────────────
        b.Entity<Tenant>(e =>
        {
            e.ToTable("tenants");
            e.HasIndex(x => x.Name);
            e.HasQueryFilter(x => x.DeletedAt == null);

            e.Property(x => x.Name).HasMaxLength(256);
            e.Property(x => x.LegalName).HasMaxLength(256);
            e.Property(x => x.TradeName).HasMaxLength(256);
            e.Property(x => x.Address).HasMaxLength(512);
            e.Property(x => x.City).HasMaxLength(128);
            e.Property(x => x.Province).HasMaxLength(128);
            e.Property(x => x.PostalCode).HasMaxLength(16);
            e.Property(x => x.Phone).HasMaxLength(64);
            e.Property(x => x.Email).HasMaxLength(256);
            e.Property(x => x.Website).HasMaxLength(256);
            e.Property(x => x.Tin).HasMaxLength(32);
            e.Property(x => x.RdoCode).HasMaxLength(8);
            e.Property(x => x.SssEmployerNumber).HasMaxLength(32);
            e.Property(x => x.PhilHealthEmployerNumber).HasMaxLength(32);
            e.Property(x => x.PagibigEmployerNumber).HasMaxLength(32);
            e.Property(x => x.DoleEstablishmentNumber).HasMaxLength(64);
            e.Property(x => x.DefaultCurrency).HasMaxLength(8);
            e.Property(x => x.DefaultTimeZone).HasMaxLength(64);
            e.Property(x => x.BillingCustomerId).HasMaxLength(128);

            e.OwnsOne(x => x.Branding, br =>
            {
                br.Property(p => p.CompanyName).HasColumnName("Brand_CompanyName").HasMaxLength(256);
                br.Property(p => p.LogoDataUrl).HasColumnName("Brand_LogoDataUrl").HasColumnType("text");
                br.Property(p => p.IsDark).HasColumnName("Brand_IsDark");
                br.Property(p => p.AccentColor).HasColumnName("Brand_AccentColor").HasMaxLength(16);
            });

            e.OwnsOne(x => x.Localization, lo =>
            {
                lo.Property(p => p.Timezone).HasColumnName("Loc_Timezone").HasMaxLength(64);
                lo.Property(p => p.DateFormat).HasColumnName("Loc_DateFormat");
                lo.Property(p => p.CurrencyCode).HasColumnName("Loc_CurrencyCode").HasMaxLength(8);
                lo.Property(p => p.CurrencySymbol).HasColumnName("Loc_CurrencySymbol").HasMaxLength(8);
                lo.Property(p => p.WeekStart).HasColumnName("Loc_WeekStart");
                lo.Property(p => p.FiscalYearStartMonth).HasColumnName("Loc_FiscalYearStartMonth");
            });

            e.OwnsOne(x => x.Payroll, pr =>
            {
                pr.Property(p => p.PayFrequency).HasColumnName("Pay_Frequency");
                pr.Property(p => p.FirstCutoffDay).HasColumnName("Pay_FirstCutoffDay");
                pr.Property(p => p.SecondCutoffDay).HasColumnName("Pay_SecondCutoffDay");
                pr.Property(p => p.RegularOvertimeRate).HasColumnName("Pay_RegularOvertimeRate").HasPrecision(6, 4);
                pr.Property(p => p.RestDayOvertimeRate).HasColumnName("Pay_RestDayOvertimeRate").HasPrecision(6, 4);
                pr.Property(p => p.HolidayOvertimeRate).HasColumnName("Pay_HolidayOvertimeRate").HasPrecision(6, 4);
                pr.Property(p => p.NightDifferentialRate).HasColumnName("Pay_NightDifferentialRate").HasPrecision(6, 4);
                pr.Property(p => p.IncludeAllowanceIn13thMonth).HasColumnName("Pay_IncludeAllowanceIn13thMonth");
                pr.Property(p => p.IncludeOtIn13thMonth).HasColumnName("Pay_IncludeOtIn13thMonth");
                pr.Property(p => p.RoundStatutoryDeductions).HasColumnName("Pay_RoundStatutoryDeductions");
            });

            e.OwnsOne(x => x.Notifications, n =>
            {
                n.Property(p => p.NewLeaveRequest).HasColumnName("Notif_NewLeaveRequest");
                n.Property(p => p.LeaveApproved).HasColumnName("Notif_LeaveApproved");
                n.Property(p => p.LeaveRejected).HasColumnName("Notif_LeaveRejected");
                n.Property(p => p.PayrollGenerated).HasColumnName("Notif_PayrollGenerated");
                n.Property(p => p.PayslipReleased).HasColumnName("Notif_PayslipReleased");
                n.Property(p => p.ContractExpiring).HasColumnName("Notif_ContractExpiring");
                n.Property(p => p.BirthdayReminder).HasColumnName("Notif_BirthdayReminder");
                n.Property(p => p.ComplianceDeadline).HasColumnName("Notif_ComplianceDeadline");
                n.Property(p => p.BenefitsRenewal).HasColumnName("Notif_BenefitsRenewal");
                n.Property(p => p.NewHireOnboarding).HasColumnName("Notif_NewHireOnboarding");
            });

            e.OwnsOne(x => x.Security, s =>
            {
                s.Property(p => p.RequireMfa).HasColumnName("Sec_RequireMfa");
                s.Property(p => p.MinPasswordLength).HasColumnName("Sec_MinPasswordLength");
                s.Property(p => p.RequireUppercase).HasColumnName("Sec_RequireUppercase");
                s.Property(p => p.RequireLowercase).HasColumnName("Sec_RequireLowercase");
                s.Property(p => p.RequireNumber).HasColumnName("Sec_RequireNumber");
                s.Property(p => p.RequireSpecial).HasColumnName("Sec_RequireSpecial");
                s.Property(p => p.PasswordExpiryDays).HasColumnName("Sec_PasswordExpiryDays");
                s.Property(p => p.SessionTimeout).HasColumnName("Sec_SessionTimeout");
                s.Property(p => p.MaxFailedLoginAttempts).HasColumnName("Sec_MaxFailedLoginAttempts");
                s.Property(p => p.IpWhitelistEnabled).HasColumnName("Sec_IpWhitelistEnabled");
                s.Property(p => p.IpWhitelist).HasColumnName("Sec_IpWhitelist").HasMaxLength(2048);
            });
        });

        // ── Identity ─────────────────────────────────────────────────────────
        b.Entity<User>(e =>
        {
            e.ToTable("users");
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Email).HasMaxLength(256).IsRequired();
            e.Property(x => x.DisplayName).HasMaxLength(128);
            e.Property(x => x.PasswordResetTokenHash).HasMaxLength(128);
            e.HasMany(x => x.Roles).WithOne().HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Cascade);
            ApplyTenantFilter(e);
        });

        b.Entity<UserRoleAssignment>(e =>
        {
            e.ToTable("user_roles");
            e.HasKey(x => new { x.UserId, x.RoleId });
            e.HasOne(x => x.Role).WithMany().HasForeignKey(x => x.RoleId);
        });

        b.Entity<Role>(e =>
        {
            e.ToTable("roles");
            e.HasIndex(x => x.Name);
            e.HasMany(x => x.Permissions).WithOne().HasForeignKey(p => p.RoleId).OnDelete(DeleteBehavior.Cascade);
            ApplyTenantFilter(e);
        });

        b.Entity<RolePermission>(e =>
        {
            e.ToTable("role_permissions");
            e.HasKey(x => new { x.RoleId, x.PermissionKey });
            e.Property(x => x.PermissionKey).HasMaxLength(64);
        });

        b.Entity<RefreshToken>(e =>
        {
            e.ToTable("refresh_tokens");
            e.HasIndex(x => x.UserId);
            e.HasIndex(x => x.TokenHash);
            ApplyTenantFilter(e);
        });

        // ── Organization ─────────────────────────────────────────────────────
        b.Entity<Department>(e =>
        {
            e.ToTable("departments");
            e.HasIndex(x => x.Code);
            ApplyTenantFilter(e);
        });

        b.Entity<Employee>(e =>
        {
            e.ToTable("employees");
            e.HasIndex(x => x.EmployeeNumber).IsUnique();
            e.HasIndex(x => x.Email);
            e.OwnsOne(x => x.PermanentAddress, a => a.ToJson());
            e.OwnsOne(x => x.CurrentAddress, a => a.ToJson());
            e.OwnsOne(x => x.Emergency, a => a.ToJson());
            e.Property(x => x.MonthlyBaseSalary).HasPrecision(18, 2);
            ApplyTenantFilter(e);
        });

        // ── Leaves ──────────────────────────────────────────────────────────
        b.Entity<LeaveType>(e =>
        {
            e.ToTable("leave_types");
            e.HasIndex(x => x.Code);
            ApplyTenantFilter(e);
        });

        b.Entity<LeaveRequest>(e =>
        {
            e.ToTable("leave_requests");
            e.HasIndex(x => x.EmployeeId);
            e.HasIndex(x => x.Status);
            e.Property(x => x.DaysRequested).HasPrecision(8, 2);
            ApplyTenantFilter(e);
        });

        b.Entity<LeaveBalance>(e =>
        {
            e.ToTable("leave_balances");
            e.HasIndex(x => new { x.EmployeeId, x.LeaveTypeId, x.PeriodYear }).IsUnique();
            e.Property(x => x.Entitlement).HasPrecision(8, 2);
            e.Property(x => x.CarryOver).HasPrecision(8, 2);
            e.Property(x => x.Used).HasPrecision(8, 2);
            e.Property(x => x.Pending).HasPrecision(8, 2);
            e.Ignore(x => x.Available);
            ApplyTenantFilter(e);
        });

        // ── Attendance ──────────────────────────────────────────────────────
        b.Entity<Shift>(e =>
        {
            e.ToTable("shifts");
            ApplyTenantFilter(e);
        });

        b.Entity<EmployeeShiftAssignment>(e =>
        {
            e.ToTable("employee_shift_assignments");
            e.HasIndex(x => new { x.EmployeeId, x.EffectiveFrom });
            ApplyTenantFilter(e);
        });

        b.Entity<AttendanceRecord>(e =>
        {
            e.ToTable("attendance_records");
            e.HasIndex(x => new { x.EmployeeId, x.Date }).IsUnique();
            ApplyTenantFilter(e);
        });

        b.Entity<OvertimeRequest>(e =>
        {
            e.ToTable("overtime_requests");
            e.HasIndex(x => x.EmployeeId);
            e.HasIndex(x => x.Status);
            ApplyTenantFilter(e);
        });

        // ── Recruitment ─────────────────────────────────────────────────────
        b.Entity<JobRequisition>(e =>
        {
            e.ToTable("job_requisitions");
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.DepartmentId);
            e.Property(x => x.Title).HasMaxLength(128).IsRequired();
            e.Property(x => x.Location).HasMaxLength(128);
            e.Property(x => x.EmploymentType).HasMaxLength(32);
            e.Property(x => x.SalaryMin).HasPrecision(18, 2);
            e.Property(x => x.SalaryMax).HasPrecision(18, 2);
            ApplyTenantFilter(e);
        });

        b.Entity<Candidate>(e =>
        {
            e.ToTable("candidates");
            e.HasIndex(x => x.JobRequisitionId);
            e.HasIndex(x => x.Stage);
            e.HasIndex(x => x.Email);
            e.Property(x => x.FirstName).HasMaxLength(64).IsRequired();
            e.Property(x => x.LastName).HasMaxLength(64).IsRequired();
            e.Property(x => x.Email).HasMaxLength(256);
            e.Property(x => x.Phone).HasMaxLength(32);
            e.Property(x => x.Source).HasMaxLength(32);
            ApplyTenantFilter(e);
        });

        b.Entity<Interview>(e =>
        {
            e.ToTable("interviews");
            e.HasIndex(x => x.CandidateId);
            e.HasIndex(x => x.ScheduledAt);
            e.Property(x => x.Location).HasMaxLength(128);
            ApplyTenantFilter(e);
        });

        // ── Payroll ─────────────────────────────────────────────────────────
        b.Entity<PayPeriod>(e =>
        {
            e.ToTable("pay_periods");
            e.HasIndex(x => x.Code);
            e.HasIndex(x => new { x.PeriodStart, x.PeriodEnd });
            e.Property(x => x.Code).HasMaxLength(32).IsRequired();
            e.Property(x => x.Frequency).HasMaxLength(16);
            ApplyTenantFilter(e);
        });

        b.Entity<Payslip>(e =>
        {
            e.ToTable("payslips");
            e.HasIndex(x => new { x.PayPeriodId, x.EmployeeId }).IsUnique();
            e.HasIndex(x => x.EmployeeId);
            e.Property(x => x.BasicSalary).HasPrecision(18, 2);
            e.Property(x => x.Overtime).HasPrecision(18, 2);
            e.Property(x => x.Bonus).HasPrecision(18, 2);
            e.Property(x => x.Allowance).HasPrecision(18, 2);
            e.Property(x => x.Sss).HasPrecision(18, 2);
            e.Property(x => x.PhilHealth).HasPrecision(18, 2);
            e.Property(x => x.PagIbig).HasPrecision(18, 2);
            e.Property(x => x.WithholdingTax).HasPrecision(18, 2);
            e.Property(x => x.LoanDeduction).HasPrecision(18, 2);
            e.Property(x => x.OtherDeduction).HasPrecision(18, 2);
            e.Ignore(x => x.Gross);
            e.Ignore(x => x.TotalDeductions);
            e.Ignore(x => x.Net);
            ApplyTenantFilter(e);
        });

        // ── Benefits ────────────────────────────────────────────────────────
        b.Entity<BenefitProgram>(e =>
        {
            e.ToTable("benefit_programs");
            e.HasIndex(x => x.Category);
            e.Property(x => x.Name).HasMaxLength(128).IsRequired();
            e.Property(x => x.Provider).HasMaxLength(128);
            e.Property(x => x.MonthlyCostPerEmployee).HasPrecision(18, 2);
            e.Property(x => x.EmployerShare).HasPrecision(18, 2);
            e.Property(x => x.EmployeeShare).HasPrecision(18, 2);
            ApplyTenantFilter(e);
        });

        b.Entity<BenefitEnrollment>(e =>
        {
            e.ToTable("benefit_enrollments");
            e.HasIndex(x => new { x.EmployeeId, x.BenefitProgramId });
            e.Property(x => x.MonthlyContribution).HasPrecision(18, 2);
            ApplyTenantFilter(e);
        });

        b.Entity<BenefitRequest>(e =>
        {
            e.ToTable("benefit_requests");
            e.HasIndex(x => x.EmployeeId);
            e.HasIndex(x => x.Status);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.MonthlyDeduction).HasPrecision(18, 2);
            e.Property(x => x.OutstandingBalance).HasPrecision(18, 2);
            ApplyTenantFilter(e);
        });

        // ── Reports ─────────────────────────────────────────────────────────
        b.Entity<ReportDefinition>(e =>
        {
            e.ToTable("report_definitions");
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Code).HasMaxLength(64);
            e.Property(x => x.Name).HasMaxLength(128).IsRequired();
            e.Property(x => x.ShortDescription).HasMaxLength(256);
            e.Property(x => x.LongDescription).HasMaxLength(2048);
            e.Property(x => x.SupportedFormatsCsv).HasMaxLength(64);
            e.Property(x => x.ColumnsCsv).HasMaxLength(2048);
            ApplyTenantFilter(e);
        });

        b.Entity<ReportSchedule>(e =>
        {
            e.ToTable("report_schedules");
            e.HasIndex(x => x.DefinitionId);
            e.HasIndex(x => x.IsActive);
            e.Property(x => x.DefinitionId).HasMaxLength(64);
            e.Property(x => x.DefinitionName).HasMaxLength(128);
            e.Property(x => x.Cadence).HasMaxLength(128);
            e.Property(x => x.RecipientsCsv).HasMaxLength(2048);
            ApplyTenantFilter(e);
        });

        b.Entity<ReportRun>(e =>
        {
            e.ToTable("report_runs");
            e.HasIndex(x => x.DefinitionId);
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.QueuedAt);
            e.Property(x => x.DefinitionId).HasMaxLength(64);
            e.Property(x => x.DefinitionName).HasMaxLength(128);
            e.Property(x => x.RanByDisplay).HasMaxLength(128);
            e.Property(x => x.DepartmentsCsv).HasMaxLength(1024);
            e.Property(x => x.FileName).HasMaxLength(256);
            e.Property(x => x.ErrorMessage).HasMaxLength(1024);
            ApplyTenantFilter(e);
        });

        b.Entity<ComplianceDeadline>(e =>
        {
            e.ToTable("compliance_deadlines");
            e.HasIndex(x => x.DueDate);
            e.HasIndex(x => x.IsFiled);
            e.Property(x => x.Agency).HasMaxLength(32);
            e.Property(x => x.FormCode).HasMaxLength(32);
            e.Property(x => x.Name).HasMaxLength(128).IsRequired();
            e.Property(x => x.Description).HasMaxLength(512);
            e.Property(x => x.PeriodCovered).HasMaxLength(64);
            e.Property(x => x.RelatedReportCode).HasMaxLength(64);
            ApplyTenantFilter(e);
        });

        // ── Outbox / Audit ──────────────────────────────────────────────────
        b.Entity<OutboxMessage>(e =>
        {
            e.ToTable("outbox_messages");
            e.HasIndex(x => x.ProcessedAt);
            ApplyTenantFilter(e);
        });

        b.Entity<AuditEvent>(e =>
        {
            e.ToTable("audit_events");
            e.HasIndex(x => new { x.EntityName, x.EntityId });
            ApplyTenantFilter(e);
        });

        // ── Notifications ───────────────────────────────────────────────────
        b.Entity<Notification>(e =>
        {
            e.ToTable("notifications");
            e.Ignore(x => x.IsRead); // computed property, not persisted
            e.Property(x => x.Title).HasMaxLength(200);
            e.Property(x => x.Body).HasMaxLength(1000);
            e.Property(x => x.RelatedEntityType).HasMaxLength(100);
            e.HasIndex(x => new { x.RecipientUserId, x.ReadAt });
            e.HasIndex(x => x.CreatedAt);
            ApplyTenantFilter(e);
        });

        // ── Postgres concurrency token (xmin system column) ─────────────────
        foreach (var entity in b.Model.GetEntityTypes()
            .Where(t => typeof(AuditableEntity).IsAssignableFrom(t.ClrType)))
        {
            b.Entity(entity.ClrType).Property(nameof(AuditableEntity.Xmin))
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();
        }
    }

    private void ApplyTenantFilter<TEntity>(EntityTypeBuilder<TEntity> e) where TEntity : AuditableEntity
    {
        e.HasQueryFilter(x => x.DeletedAt == null
            && (tenant.IsBypass || x.TenantId == tenant.CurrentTenantId));
    }

    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        var now = clock.GetUtcNow();

        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    if (entry.Entity.TenantId == Guid.Empty)
                        entry.Entity.TenantId = tenant.CurrentTenantId;
                    entry.Entity.CreatedAt   = now;
                    entry.Entity.CreatedById = user.IsAuthenticated ? user.Id : Guid.Empty;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt   = now;
                    entry.Entity.UpdatedById = user.IsAuthenticated ? user.Id : null;
                    break;

                case EntityState.Deleted:
                    // Soft delete: convert to Modified
                    entry.State = EntityState.Modified;
                    entry.Entity.DeletedAt   = now;
                    entry.Entity.DeletedById = user.IsAuthenticated ? user.Id : null;
                    break;
            }
        }

        return await base.SaveChangesAsync(ct);
    }
}
