using System.Security.Cryptography;
using System.Text;
using FluentValidation;
using Giwu.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Giwu.Application.Auth.PasswordReset;

public sealed record ResetPasswordCommand(string Email, string Token, string NewPassword)
    : IRequest<Result>;

public sealed class ResetPasswordValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8);
    }
}

internal sealed class ResetPasswordHandler(
    IApplicationDbContext db,
    IPasswordHasher hasher,
    ITenantContext tenant,
    TimeProvider clock)
    : IRequestHandler<ResetPasswordCommand, Result>
{
    public async Task<Result> Handle(ResetPasswordCommand cmd, CancellationToken ct)
    {
        tenant.Bypass();

        var user = await db.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == cmd.Email && u.DeletedAt == null, ct);

        if (user is null || !user.IsActive)
            return Result.NotFound("Invalid or expired reset token");

        if (user.PasswordResetTokenHash is null
            || user.PasswordResetExpiresAt is null
            || user.PasswordResetExpiresAt <= clock.GetUtcNow())
        {
            return Result.NotFound("Invalid or expired reset token");
        }

        var providedHash = HashToken(cmd.Token);
        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(providedHash),
                Encoding.UTF8.GetBytes(user.PasswordResetTokenHash)))
        {
            return Result.NotFound("Invalid or expired reset token");
        }

        user.PasswordHash = hasher.Hash(cmd.NewPassword);
        user.PasswordResetTokenHash = null;
        user.PasswordResetExpiresAt = null;
        user.FailedLoginCount = 0;
        user.LockedUntil = null;

        // Revoke all outstanding refresh tokens so other sessions are forced to log in again.
        var sessions = await db.RefreshTokens
            .IgnoreQueryFilters()
            .Where(t => t.UserId == user.Id && t.RevokedAt == null)
            .ToListAsync(ct);
        var now = clock.GetUtcNow();
        foreach (var s in sessions)
        {
            s.RevokedAt = now;
            s.RevokedReason = "PasswordReset";
        }

        tenant.SetTenant(user.TenantId);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
