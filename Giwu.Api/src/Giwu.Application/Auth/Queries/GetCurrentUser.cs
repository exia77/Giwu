using Giwu.Application.Common;
using Giwu.Contracts.Auth;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Giwu.Application.Auth.Queries;

/// <summary>
/// Returns the signed-in user's profile, roles, and permissions —
/// <b>read fresh from the database</b> rather than the JWT claims.
///
/// The JWT carries a snapshot taken at sign-in. If an admin changes the user's
/// role or revokes a permission, the JWT keeps the old values until expiry (~15
/// minutes). Endpoints that gate behavior on roles still trust the JWT for
/// speed, but the client's role-driven UI consults this endpoint on app boot
/// (via TryRestoreAsync) so it picks up the latest state on each launch.
/// </summary>
public sealed record GetCurrentUserQuery() : IRequest<Result<UserMeDto>>;

internal sealed class GetCurrentUserHandler(
    IApplicationDbContext db,
    ICurrentUser user)
    : IRequestHandler<GetCurrentUserQuery, Result<UserMeDto>>
{
    public async Task<Result<UserMeDto>> Handle(GetCurrentUserQuery q, CancellationToken ct)
    {
        if (!user.IsAuthenticated)
            return Result<UserMeDto>.NotFound();

        var dbUser = await db.Users
            .Include(u => u.Roles).ThenInclude(r => r.Role).ThenInclude(r => r.Permissions)
            .FirstOrDefaultAsync(u => u.Id == user.Id, ct);

        // Hard-deleted, soft-deleted by the tenant filter, or DB-disabled — treat
        // any of these as "no longer valid" so the client redirects to login.
        if (dbUser is null || !dbUser.IsActive)
            return Result<UserMeDto>.NotFound();

        var roles = dbUser.Roles.Select(r => r.Role.Name).ToArray();
        var perms = dbUser.Roles
            .SelectMany(r => r.Role.Permissions.Select(p => p.PermissionKey))
            .Distinct()
            .ToArray();

        return Result<UserMeDto>.Success(new UserMeDto(
            dbUser.Id, dbUser.TenantId, dbUser.EmployeeId,
            dbUser.Email, dbUser.DisplayName,
            roles, perms));
    }
}
