using Giwu.Application.Common;
using Giwu.Contracts.Auth;
using Giwu.Domain.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Giwu.Application.Auth.Refresh;

public sealed record RefreshTokenCommand(string RefreshToken)
    : IRequest<Result<LoginResponse>>;

internal sealed class RefreshTokenHandler(
    IApplicationDbContext db,
    IJwtTokenService jwt,
    ITenantContext tenant,
    TimeProvider clock)
    : IRequestHandler<RefreshTokenCommand, Result<LoginResponse>>
{
    public async Task<Result<LoginResponse>> Handle(RefreshTokenCommand cmd, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cmd.RefreshToken))
            return Result<LoginResponse>.NotFound("Invalid refresh token");

        tenant.Bypass();

        var hash = jwt.HashRefreshToken(cmd.RefreshToken);
        var now  = clock.GetUtcNow();

        var token = await db.RefreshTokens
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t =>
                t.TokenHash == hash &&
                t.RevokedAt == null &&
                t.ExpiresAt > now &&
                t.DeletedAt == null, ct);

        if (token is null)
            return Result<LoginResponse>.NotFound("Invalid or expired refresh token");

        var user = await db.Users
            .Include(u => u.Roles).ThenInclude(r => r.Role).ThenInclude(r => r.Permissions)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == token.UserId && u.DeletedAt == null, ct);

        if (user is null || !user.IsActive)
            return Result<LoginResponse>.Forbidden("User is no longer active");

        // Rotate: revoke old, issue new
        token.RevokedAt = now;
        token.RevokedReason = "Rotated";

        var roles = user.Roles.Select(r => r.Role.Name).ToArray();
        var perms = user.Roles
            .SelectMany(r => r.Role.Permissions.Select(p => p.PermissionKey))
            .Distinct()
            .ToArray();

        tenant.SetTenant(user.TenantId);

        var (access, exp) = jwt.IssueAccessToken(
            user.Id, user.TenantId, user.EmployeeId,
            user.Email, user.DisplayName, roles, perms);

        var newRefresh = jwt.IssueRefreshToken();
        db.RefreshTokens.Add(new RefreshToken
        {
            UserId    = user.Id,
            TenantId  = user.TenantId,
            TokenHash = jwt.HashRefreshToken(newRefresh),
            ExpiresAt = now.AddDays(7),
        });

        await db.SaveChangesAsync(ct);

        return Result<LoginResponse>.Success(new LoginResponse(
            access, newRefresh, exp,
            new UserMeDto(user.Id, user.TenantId, user.EmployeeId,
                          user.Email, user.DisplayName, roles, perms)));
    }
}
