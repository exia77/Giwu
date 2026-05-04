using FluentValidation;
using Giwu.Application.Common;
using Giwu.Contracts.Auth;
using Giwu.Domain.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Giwu.Application.Auth.Login;

public sealed record LoginCommand(string Email, string Password)
    : IRequest<Result<LoginResponse>>;

public sealed class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
    }
}

internal sealed class LoginHandler(
    IApplicationDbContext db,
    IPasswordHasher hasher,
    IJwtTokenService jwt,
    ITenantContext tenant,
    TimeProvider clock)
    : IRequestHandler<LoginCommand, Result<LoginResponse>>
{
    public async Task<Result<LoginResponse>> Handle(LoginCommand cmd, CancellationToken ct)
    {
        // Login crosses tenants: bypass the tenant filter so we can find the user.
        tenant.Bypass();

        var user = await db.Users
            .Include(u => u.Roles).ThenInclude(r => r.Role).ThenInclude(r => r.Permissions)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == cmd.Email && u.DeletedAt == null, ct);

        if (user is null || !user.IsActive)
            return Result<LoginResponse>.NotFound("Invalid credentials");

        if (user.LockedUntil is { } until && until > clock.GetUtcNow())
            return Result<LoginResponse>.Forbidden("Account temporarily locked");

        if (!hasher.Verify(cmd.Password, user.PasswordHash))
        {
            user.FailedLoginCount++;
            if (user.FailedLoginCount >= 5)
                user.LockedUntil = clock.GetUtcNow().AddMinutes(15);
            await db.SaveChangesAsync(ct);
            return Result<LoginResponse>.NotFound("Invalid credentials");
        }

        // Reset lockout
        user.FailedLoginCount = 0;
        user.LockedUntil = null;
        user.LastLoginAt = clock.GetUtcNow();

        var roles = user.Roles.Select(r => r.Role.Name).ToArray();
        var perms = user.Roles
            .SelectMany(r => r.Role.Permissions.Select(p => p.PermissionKey))
            .Distinct()
            .ToArray();

        // Now scope to user's tenant for everything that follows
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
