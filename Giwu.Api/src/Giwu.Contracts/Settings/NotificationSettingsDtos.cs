using Giwu.Domain.Tenancy;

namespace Giwu.Contracts.Settings;

public sealed record NotificationSettingsDto(
    NotificationChannel NewLeaveRequest,
    NotificationChannel LeaveApproved,
    NotificationChannel LeaveRejected,
    NotificationChannel PayrollGenerated,
    NotificationChannel PayslipReleased,
    NotificationChannel ContractExpiring,
    NotificationChannel BirthdayReminder,
    NotificationChannel ComplianceDeadline,
    NotificationChannel BenefitsRenewal,
    NotificationChannel NewHireOnboarding);

public sealed record UpdateNotificationSettingsRequest(
    NotificationChannel NewLeaveRequest,
    NotificationChannel LeaveApproved,
    NotificationChannel LeaveRejected,
    NotificationChannel PayrollGenerated,
    NotificationChannel PayslipReleased,
    NotificationChannel ContractExpiring,
    NotificationChannel BirthdayReminder,
    NotificationChannel ComplianceDeadline,
    NotificationChannel BenefitsRenewal,
    NotificationChannel NewHireOnboarding);
