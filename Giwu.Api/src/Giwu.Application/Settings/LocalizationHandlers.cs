using FluentValidation;
using Giwu.Application.Common;
using Giwu.Contracts.Settings;
using Giwu.Domain.Tenancy;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Giwu.Application.Settings;

// ── Get ─────────────────────────────────────────────────────────────────────
public sealed record GetLocalizationSettingsQuery : IRequest<Result<LocalizationSettingsDto>>;

internal sealed class GetLocalizationSettingsHandler(IApplicationDbContext db, ICurrentUser user)
    : IRequestHandler<GetLocalizationSettingsQuery, Result<LocalizationSettingsDto>>
{
    public async Task<Result<LocalizationSettingsDto>> Handle(GetLocalizationSettingsQuery _, CancellationToken ct)
    {
        if (!user.IsAuthenticated) return Result<LocalizationSettingsDto>.Forbidden();

        var t = await db.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == user.TenantId && x.DeletedAt == null, ct);

        if (t is null) return Result<LocalizationSettingsDto>.NotFound();

        var l = t.Localization ?? new LocalizationSettings();
        return Result<LocalizationSettingsDto>.Success(new LocalizationSettingsDto(
            l.Timezone, l.DateFormat, l.CurrencyCode, l.CurrencySymbol,
            l.WeekStart, l.FiscalYearStartMonth));
    }
}

// ── Update ──────────────────────────────────────────────────────────────────
public sealed record UpdateLocalizationSettingsCommand(UpdateLocalizationSettingsRequest Request)
    : IRequest<Result<LocalizationSettingsDto>>;

public sealed class UpdateLocalizationSettingsValidator : AbstractValidator<UpdateLocalizationSettingsCommand>
{
    public UpdateLocalizationSettingsValidator()
    {
        RuleFor(x => x.Request.Timezone).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Request.CurrencyCode).NotEmpty().MaximumLength(8);
        RuleFor(x => x.Request.CurrencySymbol).NotEmpty().MaximumLength(8);
        RuleFor(x => x.Request.FiscalYearStartMonth).InclusiveBetween(1, 12);
        RuleFor(x => x.Request.DateFormat).IsInEnum();
        RuleFor(x => x.Request.WeekStart).IsInEnum();
    }
}

internal sealed class UpdateLocalizationSettingsHandler(IApplicationDbContext db, ICurrentUser user)
    : IRequestHandler<UpdateLocalizationSettingsCommand, Result<LocalizationSettingsDto>>
{
    public async Task<Result<LocalizationSettingsDto>> Handle(UpdateLocalizationSettingsCommand cmd, CancellationToken ct)
    {
        if (!user.IsAuthenticated) return Result<LocalizationSettingsDto>.Forbidden();

        var t = await db.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == user.TenantId && x.DeletedAt == null, ct);

        if (t is null) return Result<LocalizationSettingsDto>.NotFound();

        t.Localization ??= new LocalizationSettings();
        var l = t.Localization;
        var r = cmd.Request;
        l.Timezone             = r.Timezone;
        l.DateFormat           = r.DateFormat;
        l.CurrencyCode         = r.CurrencyCode;
        l.CurrencySymbol       = r.CurrencySymbol;
        l.WeekStart            = r.WeekStart;
        l.FiscalYearStartMonth = r.FiscalYearStartMonth;

        // Mirror to the legacy top-level fields so /api/tenants/me stays consistent.
        t.DefaultCurrency = r.CurrencyCode;
        t.DefaultTimeZone = r.Timezone;

        await db.SaveChangesAsync(ct);

        return Result<LocalizationSettingsDto>.Success(new LocalizationSettingsDto(
            l.Timezone, l.DateFormat, l.CurrencyCode, l.CurrencySymbol,
            l.WeekStart, l.FiscalYearStartMonth));
    }
}
