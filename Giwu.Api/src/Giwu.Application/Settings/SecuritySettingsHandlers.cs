using FluentValidation;
using Giwu.Application.Common;
using Giwu.Contracts.Settings;
using Giwu.Domain.Tenancy;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Giwu.Application.Settings;

// ── Get ─────────────────────────────────────────────────────────────────────
public sealed record GetSecuritySettingsQuery : IRequest<Result<SecuritySettingsDto>>;

internal sealed class GetSecuritySettingsHandler(IApplicationDbContext db, ICurrentUser user)
    : IRequestHandler<GetSecuritySettingsQuery, Result<SecuritySettingsDto>>
{
    public async Task<Result<SecuritySettingsDto>> Handle(GetSecuritySettingsQuery _, CancellationToken ct)
    {
        if (!user.IsAuthenticated) return Result<SecuritySettingsDto>.Forbidden();

        var t = await db.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == user.TenantId && x.DeletedAt == null, ct);

        if (t is null) return Result<SecuritySettingsDto>.NotFound();

        var s = t.Security ?? new SecuritySettings();
        return Result<SecuritySettingsDto>.Success(SecuritySettingsMapper.ToDto(s));
    }
}

// ── Update ──────────────────────────────────────────────────────────────────
public sealed record UpdateSecuritySettingsCommand(UpdateSecuritySettingsRequest Request)
    : IRequest<Result<SecuritySettingsDto>>;

public sealed class UpdateSecuritySettingsValidator : AbstractValidator<UpdateSecuritySettingsCommand>
{
    public UpdateSecuritySettingsValidator()
    {
        RuleFor(x => x.Request.MinPasswordLength).InclusiveBetween(6, 64);
        RuleFor(x => x.Request.PasswordExpiryDays).InclusiveBetween(0, 365);
        RuleFor(x => x.Request.MaxFailedLoginAttempts).InclusiveBetween(3, 20);
        RuleFor(x => x.Request.SessionTimeout).IsInEnum();
        RuleFor(x => x.Request.IpWhitelist).MaximumLength(2048);
    }
}

internal sealed class UpdateSecuritySettingsHandler(IApplicationDbContext db, ICurrentUser user)
    : IRequestHandler<UpdateSecuritySettingsCommand, Result<SecuritySettingsDto>>
{
    public async Task<Result<SecuritySettingsDto>> Handle(UpdateSecuritySettingsCommand cmd, CancellationToken ct)
    {
        if (!user.IsAuthenticated) return Result<SecuritySettingsDto>.Forbidden();

        var t = await db.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == user.TenantId && x.DeletedAt == null, ct);

        if (t is null) return Result<SecuritySettingsDto>.NotFound();

        t.Security ??= new SecuritySettings();
        var s = t.Security;
        var r = cmd.Request;
        s.RequireMfa             = r.RequireMfa;
        s.MinPasswordLength      = r.MinPasswordLength;
        s.RequireUppercase       = r.RequireUppercase;
        s.RequireLowercase       = r.RequireLowercase;
        s.RequireNumber          = r.RequireNumber;
        s.RequireSpecial         = r.RequireSpecial;
        s.PasswordExpiryDays     = r.PasswordExpiryDays;
        s.SessionTimeout         = r.SessionTimeout;
        s.MaxFailedLoginAttempts = r.MaxFailedLoginAttempts;
        s.IpWhitelistEnabled     = r.IpWhitelistEnabled;
        s.IpWhitelist            = r.IpWhitelist ?? "";

        await db.SaveChangesAsync(ct);
        return Result<SecuritySettingsDto>.Success(SecuritySettingsMapper.ToDto(s));
    }
}

internal static class SecuritySettingsMapper
{
    public static SecuritySettingsDto ToDto(SecuritySettings s) => new(
        s.RequireMfa, s.MinPasswordLength,
        s.RequireUppercase, s.RequireLowercase, s.RequireNumber, s.RequireSpecial,
        s.PasswordExpiryDays, s.SessionTimeout, s.MaxFailedLoginAttempts,
        s.IpWhitelistEnabled, s.IpWhitelist);
}
