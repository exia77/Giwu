using Microsoft.EntityFrameworkCore;
using Giwu.Domain.Attendance;
using Giwu.Domain.Audit;
using Giwu.Domain.Benefits;
using Giwu.Domain.Identity;
using Giwu.Domain.Leaves;
using Giwu.Domain.Organization;
using Giwu.Domain.Outbox;
using Giwu.Domain.Payroll;
using Giwu.Domain.Recruitment;
using Giwu.Domain.Tenancy;

namespace Giwu.Application.Common;

public interface IApplicationDbContext
{
    DbSet<Tenant>          Tenants          { get; }
    DbSet<User>            Users            { get; }
    DbSet<Role>            Roles            { get; }
    DbSet<RolePermission>  RolePermissions  { get; }
    DbSet<RefreshToken>    RefreshTokens    { get; }
    DbSet<Department>      Departments      { get; }
    DbSet<Employee>        Employees        { get; }
    DbSet<LeaveType>       LeaveTypes       { get; }
    DbSet<LeaveRequest>    LeaveRequests    { get; }
    DbSet<LeaveBalance>    LeaveBalances    { get; }
    DbSet<Shift>                    Shifts                   { get; }
    DbSet<EmployeeShiftAssignment>  EmployeeShiftAssignments { get; }
    DbSet<AttendanceRecord>         AttendanceRecords        { get; }
    DbSet<OvertimeRequest>          OvertimeRequests         { get; }
    DbSet<OutboxMessage>   Outbox           { get; }
    DbSet<AuditEvent>      AuditEvents      { get; }

    DbSet<JobRequisition>     JobRequisitions     { get; }
    DbSet<Candidate>          Candidates          { get; }
    DbSet<Interview>          Interviews          { get; }

    DbSet<PayPeriod>          PayPeriods          { get; }
    DbSet<Payslip>            Payslips            { get; }

    DbSet<BenefitProgram>     BenefitPrograms     { get; }
    DbSet<BenefitEnrollment>  BenefitEnrollments  { get; }
    DbSet<BenefitRequest>     BenefitRequests     { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

public interface ITenantContext
{
    Guid CurrentTenantId { get; }
    bool IsBypass { get; }                  // for system jobs that scan all tenants
    void SetTenant(Guid tenantId);
    void Bypass();
}

public interface ICurrentUser
{
    Guid Id { get; }
    Guid TenantId { get; }
    Guid? EmployeeId { get; }
    string Email { get; }
    string DisplayName { get; }
    IReadOnlyCollection<string> Roles { get; }
    IReadOnlyCollection<string> Permissions { get; }
    bool IsAuthenticated { get; }
}

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

public interface IJwtTokenService
{
    (string AccessToken, DateTimeOffset ExpiresAt) IssueAccessToken(
        Guid userId, Guid tenantId, Guid? employeeId, string email, string displayName,
        IEnumerable<string> roles, IEnumerable<string> permissions);

    string IssueRefreshToken();
    string HashRefreshToken(string token);
}
