using FluentValidation;
using Giwu.Application.Common;
using Giwu.Contracts.Settings;
using Giwu.Domain.Tenancy;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Giwu.Application.Settings;

// ── Get ─────────────────────────────────────────────────────────────────────
public sealed record GetBrandingSettingsQuery : IRequest<Result<BrandingSettingsDto>>;

internal sealed class GetBrandingSettingsHandler(IApplicationDbContext db, ICurrentUser user)
    : IRequestHandler<GetBrandingSettingsQuery, Result<BrandingSettingsDto>>
{
    public async Task<Result<BrandingSettingsDto>> Handle(GetBrandingSettingsQuery _, CancellationToken ct)
    {
        if (!user.IsAuthenticated) return Result<BrandingSettingsDto>.Forbidden();

        var t = await db.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == user.TenantId && x.DeletedAt == null, ct);

        if (t is null) return Result<BrandingSettingsDto>.NotFound();

        var b = t.Branding ?? new BrandingSettings();
        return Result<BrandingSettingsDto>.Success(BrandingSettingsMapper.ToDto(b));
    }
}

// ── Update ──────────────────────────────────────────────────────────────────
public sealed record UpdateBrandingSettingsCommand(UpdateBrandingSettingsRequest Request)
    : IRequest<Result<BrandingSettingsDto>>;

public sealed class UpdateBrandingSettingsValidator : AbstractValidator<UpdateBrandingSettingsCommand>
{
    public UpdateBrandingSettingsValidator()
    {
        RuleFor(x => x.Request.CompanyName).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Request.AccentColor).NotEmpty().MaximumLength(16)
            .Matches("^#[0-9A-Fa-f]{6}$").WithMessage("AccentColor must be a 6-digit hex like #1D9E75.");
        // LogoDataUrl is intentionally unbounded — base64 PNG/SVG can run large.
    }
}

internal sealed class UpdateBrandingSettingsHandler(IApplicationDbContext db, ICurrentUser user)
    : IRequestHandler<UpdateBrandingSettingsCommand, Result<BrandingSettingsDto>>
{
    public async Task<Result<BrandingSettingsDto>> Handle(UpdateBrandingSettingsCommand cmd, CancellationToken ct)
    {
        if (!user.IsAuthenticated) return Result<BrandingSettingsDto>.Forbidden();

        var t = await db.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == user.TenantId && x.DeletedAt == null, ct);

        if (t is null) return Result<BrandingSettingsDto>.NotFound();

        t.Branding ??= new BrandingSettings();
        var b = t.Branding;
        var r = cmd.Request;
        b.CompanyName = r.CompanyName;
        b.LogoDataUrl = r.LogoDataUrl ?? "";
        b.IsDark      = r.IsDark;
        b.AccentColor = r.AccentColor;

        await db.SaveChangesAsync(ct);
        return Result<BrandingSettingsDto>.Success(BrandingSettingsMapper.ToDto(b));
    }
}

internal static class BrandingSettingsMapper
{
    public static BrandingSettingsDto ToDto(BrandingSettings b) =>
        new(b.CompanyName, b.LogoDataUrl, b.IsDark, b.AccentColor);
}
