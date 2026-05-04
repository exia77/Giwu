using Giwu.Application.Common;

namespace Giwu.Infrastructure.Tenancy;

/// <summary>
/// Per-request tenant context. Resolved from the JWT 'tid' claim by
/// TenantMiddleware on the API side, then passed into the DbContext for the
/// global query filter. System jobs call <see cref="Bypass"/> to scan all
/// tenants (e.g. nightly attendance rollup).
/// </summary>
public sealed class TenantContext : ITenantContext
{
    public Guid CurrentTenantId { get; private set; } = Guid.Empty;
    public bool IsBypass { get; private set; }

    public void SetTenant(Guid tenantId)
    {
        CurrentTenantId = tenantId;
        IsBypass = false;
    }

    public void Bypass() => IsBypass = true;
}
