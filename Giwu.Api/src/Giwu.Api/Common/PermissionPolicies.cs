using Giwu.Domain.Identity;
using Microsoft.AspNetCore.Authorization;

namespace Giwu.Api.Common;

/// <summary>
/// Registers one Authorization policy per permission key. Endpoints declare
/// <c>.RequireAuthorization(Permissions.Employees.View)</c> and the JWT's
/// <c>perm</c> claims must include the matching key.
/// </summary>
public static class PermissionPolicies
{
    public static AuthorizationOptions RegisterPermissionPolicies(this AuthorizationOptions opts)
    {
        foreach (var key in Permissions.All)
        {
            opts.AddPolicy(key, p => p.RequireAssertion(ctx =>
                ctx.User.HasClaim("perm", key)));
        }
        return opts;
    }
}
