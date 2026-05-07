using FluentValidation;
using Giwu.Application.Common;
using Giwu.Contracts.Settings;
using Giwu.Domain.Tenancy;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Giwu.Application.Settings;

// ── Get ─────────────────────────────────────────────────────────────────────
public sealed record GetNotificationSettingsQuery : IRequest<Result<NotificationSettingsDto>>;

internal sealed class GetNotificationSettingsHandler(IApplicationDbContext db, ICurrentUser user)
    : IRequestHandler<GetNotificationSettingsQuery, Result<NotificationSettingsDto>>
{
    public async Task<Result<NotificationSettingsDto>> Handle(GetNotificationSettingsQuery _, CancellationToken ct)
    {
        if (!user.IsAuthenticated) return Result<NotificationSettingsDto>.Forbidden();

        var t = await db.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == user.TenantId && x.DeletedAt == null, ct);

        if (t is null) return Result<NotificationSettingsDto>.NotFound();

        var n = t.Notifications ?? new NotificationSettings();
        return Result<NotificationSettingsDto>.Success(NotificationSettingsMapper.ToDto(n));
    }
}

// ── Update ──────────────────────────────────────────────────────────────────
public sealed record UpdateNotificationSettingsCommand(UpdateNotificationSettingsRequest Request)
    : IRequest<Result<NotificationSettingsDto>>;

public sealed class UpdateNotificationSettingsValidator : AbstractValidator<UpdateNotificationSettingsCommand>
{
    public UpdateNotificationSettingsValidator()
    {
        RuleFor(x => x.Request.NewLeaveRequest).IsInEnum();
        RuleFor(x => x.Request.LeaveApproved).IsInEnum();
        RuleFor(x => x.Request.LeaveRejected).IsInEnum();
        RuleFor(x => x.Request.PayrollGenerated).IsInEnum();
        RuleFor(x => x.Request.PayslipReleased).IsInEnum();
        RuleFor(x => x.Request.ContractExpiring).IsInEnum();
        RuleFor(x => x.Request.BirthdayReminder).IsInEnum();
        RuleFor(x => x.Request.ComplianceDeadline).IsInEnum();
        RuleFor(x => x.Request.BenefitsRenewal).IsInEnum();
        RuleFor(x => x.Request.NewHireOnboarding).IsInEnum();
    }
}

internal sealed class UpdateNotificationSettingsHandler(IApplicationDbContext db, ICurrentUser user)
    : IRequestHandler<UpdateNotificationSettingsCommand, Result<NotificationSettingsDto>>
{
    public async Task<Result<NotificationSettingsDto>> Handle(UpdateNotificationSettingsCommand cmd, CancellationToken ct)
    {
        if (!user.IsAuthenticated) return Result<NotificationSettingsDto>.Forbidden();

        var t = await db.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == user.TenantId && x.DeletedAt == null, ct);

        if (t is null) return Result<NotificationSettingsDto>.NotFound();

        t.Notifications ??= new NotificationSettings();
        var n = t.Notifications;
        var r = cmd.Request;
        n.NewLeaveRequest    = r.NewLeaveRequest;
        n.LeaveApproved      = r.LeaveApproved;
        n.LeaveRejected      = r.LeaveRejected;
        n.PayrollGenerated   = r.PayrollGenerated;
        n.PayslipReleased    = r.PayslipReleased;
        n.ContractExpiring   = r.ContractExpiring;
        n.BirthdayReminder   = r.BirthdayReminder;
        n.ComplianceDeadline = r.ComplianceDeadline;
        n.BenefitsRenewal    = r.BenefitsRenewal;
        n.NewHireOnboarding  = r.NewHireOnboarding;

        await db.SaveChangesAsync(ct);
        return Result<NotificationSettingsDto>.Success(NotificationSettingsMapper.ToDto(n));
    }
}

internal static class NotificationSettingsMapper
{
    public static NotificationSettingsDto ToDto(NotificationSettings n) => new(
        n.NewLeaveRequest, n.LeaveApproved, n.LeaveRejected,
        n.PayrollGenerated, n.PayslipReleased,
        n.ContractExpiring, n.BirthdayReminder,
        n.ComplianceDeadline, n.BenefitsRenewal, n.NewHireOnboarding);
}
