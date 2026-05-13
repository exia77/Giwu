using FluentValidation;
using Giwu.Application.Common;
using Giwu.Contracts.Auth;
using Giwu.Domain.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Giwu.Application.Auth.Google;

public sealed record GoogleSignInCommand(string IdToken)
    : IRequest<Result<LoginResponse>>;

public sealed class GoogleSignInValidator : AbstractValidator<GoogleSignInCommand>
{
    public GoogleSignInValidator()
    {
        RuleFor(x => x.IdToken).NotEmpty();
    }
}

internal sealed class GoogleSignInHandler(
    IApplicationDbContext db,
    IGoogleTokenVerifier verifier,
    IJwtTokenService jwt,
    ITenantContext tenant,
    TimeProvider clock)
    : IRequestHandler<GoogleSignInCommand, Result<LoginResponse>>
{
    public async Task<Result<LoginResponse>> Handle(GoogleSignInCommand cmd, CancellationToken ct)
    {
        var google = await verifier.VerifyAsync(cmd.IdToken, ct);
        if (google is null || string.IsNullOrEmpty(google.Email))
            return Result<LoginResponse>.Forbidden("Google sign-in failed");

        if (!google.EmailVerified)
            return Result<LoginResponse>.Forbidden("Your Google email is not verified");

        tenant.Bypass();

        var user = await db.Users
            .Include(u => u.Roles).ThenInclude(r => r.Role).ThenInclude(r => r.Permissions)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == google.Email && u.DeletedAt == null, ct);

        // Only existing users can sign in with Google. Provisioning is out of scope —
        // an admin must create the account first.
        if (user is null || !user.IsActive)
            return Result<LoginResponse>.NotFound("No account is linked to this Google email");

        user.FailedLoginCount = 0;
        user.LockedUntil = null;
        user.LastLoginAt = clock.GetUtcNow();

        var roles = user.Roles.Select(r => r.Role.Name).ToArray();
        var perms = user.Roles
            .SelectMany(r => r.Role.Permissions.Select(p => p.PermissionKey))
            .Distinct()
            .ToArray();

        tenant.SetTenant(user.TenantId);

        var (access, exp) = jwt.IssueAccessToken(
            user.Id, user.TenantId, user.EmployeeId,
            user.Email, user.DisplayName, roles, perms);

        var refresh = jwt.IssueRefreshToken();
        db.RefreshTokens.Add(new RefreshToken
        {
            UserId    = user.Id,
            TenantId  = user.TenantId,
            TokenHash = jwt.HashRefreshToken(refresh),
            ExpiresAt = clock.GetUtcNow().AddDays(7),
        });
        await db.SaveChangesAsync(ct);

        return Result<LoginResponse>.Success(new LoginResponse(
            access, refresh, exp,
            new UserMeDto(user.Id, user.TenantId, user.EmployeeId,
                          user.Email, user.DisplayName, roles, perms)));
    }
}
