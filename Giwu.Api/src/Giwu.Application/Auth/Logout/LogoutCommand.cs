using Giwu.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Giwu.Application.Auth.Logout;

public sealed record LogoutCommand(string? RefreshToken, bool AllSessions = false) : IRequest<Result>;

internal sealed class LogoutHandler(
    IApplicationDbContext db,
    ICurrentUser user,
    IJwtTokenService jwt,
    ITenantContext tenant,
    TimeProvider clock)
    : IRequestHandler<LogoutCommand, Result>
{
    public async Task<Result> Handle(LogoutCommand cmd, CancellationToken ct)
    {
        if (!user.IsAuthenticated) return Result.Forbidden();

        tenant.Bypass();
        var now = clock.GetUtcNow();

        if (cmd.AllSessions)
        {
            var all = await db.RefreshTokens
                .Where(t => t.UserId == user.Id && t.RevokedAt == null)
                .ToListAsync(ct);
            foreach (var t in all)
            {
                t.RevokedAt = now;
                t.RevokedReason = "Logout-all";
            }
        }
        else if (!string.IsNullOrEmpty(cmd.RefreshToken))
        {
            var hash = jwt.HashRefreshToken(cmd.RefreshToken);
            var token = await db.RefreshTokens.FirstOrDefaultAsync(
                t => t.UserId == user.Id && t.TokenHash == hash && t.RevokedAt == null, ct);
            if (token is not null)
            {
                token.RevokedAt = now;
                token.RevokedReason = "Logout";
            }
        }

        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
