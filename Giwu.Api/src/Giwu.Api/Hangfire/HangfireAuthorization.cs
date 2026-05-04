using Giwu.Domain.Identity;
using Hangfire.Dashboard;

namespace Giwu.Api.Hangfire;

/// <summary>
/// Restricts the /hangfire dashboard to authenticated users with the
/// Settings.Manage permission. In production, also restrict by IP or VPN.
/// </summary>
public sealed class HangfireDashboardAuthorization : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var http = context.GetHttpContext();
        return http.User.Identity?.IsAuthenticated == true
            && http.User.HasClaim("perm", Permissions.Settings.Manage);
    }
}
