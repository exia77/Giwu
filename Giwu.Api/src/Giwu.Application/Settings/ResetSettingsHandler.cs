using Giwu.Application.Common;
using Giwu.Domain.Tenancy;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Giwu.Application.Settings;

/// <summary>
/// Resets Branding, Localization, Payroll defaults, Notification preferences, and
/// Security settings to factory values. Organization and PH employer registration
/// numbers are intentionally NOT reset — those are legal-entity facts, not preferences.
/// </summary>
public sealed record ResetSettingsCommand : IRequest<Result>;

internal sealed class ResetSettingsHandler(IApplicationDbContext db, ICurrentUser user)
    : IRequestHandler<ResetSettingsCommand, Result>
{
    public async Task<Result> Handle(ResetSettingsCommand _, CancellationToken ct)
    {
        if (!user.IsAuthenticated) return Result.Forbidden();

        var t = await db.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == user.TenantId && x.DeletedAt == null, ct);

        if (t is null) return Result.NotFound();

        t.Branding      = new BrandingSettings();
        t.Localization  = new LocalizationSettings();
        t.Payroll       = new PayrollDefaults();
        t.Notifications = new NotificationSettings();
        t.Security      = new SecuritySettings();

        // Mirror the legacy top-level fields back to the new defaults too.
        t.DefaultCurrency = t.Localization.CurrencyCode;
        t.DefaultTimeZone = t.Localization.Timezone;

        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
