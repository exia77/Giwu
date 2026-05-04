using Giwu.Application.Common;

namespace Giwu.Api.Middleware;

/// <summary>
/// Reads the 'tid' claim from the authenticated request and pushes it onto the
/// per-request <see cref="ITenantContext"/>. Runs after authentication.
/// </summary>
public sealed class TenantMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext ctx, ITenantContext tenant)
    {
        var tid = ctx.User.FindFirst("tid")?.Value;
        if (Guid.TryParse(tid, out var tenantId))
            tenant.SetTenant(tenantId);
        await next(ctx);
    }
}
