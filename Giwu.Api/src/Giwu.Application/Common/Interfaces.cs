using Microsoft.EntityFrameworkCore;
using Giwu.Domain.Attendance;
using Giwu.Domain.Audit;
using Giwu.Domain.Benefits;
using Giwu.Domain.Identity;
using Giwu.Domain.Leaves;
using Giwu.Domain.Notifications;
using Giwu.Domain.Organization;
using Giwu.Domain.Outbox;
using Giwu.Domain.Payroll;
using Giwu.Domain.Recruitment;
using Giwu.Domain.Reports;
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

    DbSet<ReportDefinition>      ReportDefinitions     { get; }
    DbSet<ReportSchedule>        ReportSchedules       { get; }
    DbSet<ReportRun>             ReportRuns            { get; }
    DbSet<ComplianceDeadline>    ComplianceDeadlines   { get; }

    DbSet<Notification>          Notifications         { get; }

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

public interface IEmailSender
{
    Task SendAsync(string toEmail, string toName, string subject, string htmlBody, CancellationToken ct = default);
}

/// <summary>
/// Talks to the payment provider (Stripe). Kept behind an interface so we
/// can swap to PayMongo or stub it for tests without touching handlers.
/// </summary>
public interface IBillingService
{
    /// <summary>True when the provider is configured (secret key + price IDs set).</summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Creates a Stripe Checkout Session for the target tier. Returns the URL the
    /// user should be redirected to. Lazily creates a Stripe Customer for the tenant
    /// on first call and stores the id on <c>Tenant.BillingCustomerId</c>.
    /// </summary>
    Task<string> CreateCheckoutSessionAsync(
        Giwu.Domain.Tenancy.SubscriptionTier targetTier,
        Guid tenantId,
        string tenantName,
        string? tenantEmail,
        CancellationToken ct = default);

    /// <summary>
    /// Verifies a Stripe webhook signature and returns a normalized event the
    /// handler can act on. Returns null if the payload/signature is invalid —
    /// callers MUST treat that as a 400.
    /// </summary>
    BillingWebhookEvent? VerifyWebhook(string payload, string stripeSignatureHeader);

    /// <summary>
    /// Maps a Stripe Price ID back to one of our subscription tiers. Used by the
    /// webhook handler to know what tier the user just paid for.
    /// </summary>
    Giwu.Domain.Tenancy.SubscriptionTier? TierForPriceId(string priceId);
}

/// <summary>
/// Provider-agnostic shape the webhook handler reasons about. Today only
/// Stripe is wired in; if we add PayMongo later it'd emit the same shape.
/// </summary>
public sealed record BillingWebhookEvent(
    string EventId,
    string Type,
    string? CustomerId,
    string? PriceId,
    string? SubscriptionStatus,
    DateTimeOffset? CurrentPeriodEndsAt);

public interface IGoogleTokenVerifier
{
    /// <summary>
    /// Validates a Google ID token issued for this app's OAuth client.
    /// Returns null if the token is invalid, expired, or for a different audience.
    /// </summary>
    Task<GoogleUserInfo?> VerifyAsync(string idToken, CancellationToken ct = default);
}

/// <summary>
/// Exchanges a Google OAuth authorization code (server-side flow) for an ID
/// token. Used by the MAUI Hybrid client because its WebView origin can't run
/// the in-app GIS popup — the system browser handles the auth dance instead.
/// </summary>
public interface IGoogleCodeExchanger
{
    /// <summary>
    /// Calls Google's token endpoint with the authorization code. Returns the
    /// id_token on success, null if the code is invalid/expired or the exchange
    /// otherwise failed.
    /// </summary>
    Task<string?> ExchangeCodeForIdTokenAsync(string code, CancellationToken ct = default);
}

public sealed record GoogleUserInfo(
    string Subject,
    string Email,
    bool EmailVerified,
    string DisplayName,
    string? PictureUrl);
