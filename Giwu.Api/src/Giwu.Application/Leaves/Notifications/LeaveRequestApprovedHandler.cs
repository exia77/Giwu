using Giwu.Domain.Leaves;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Giwu.Application.Leaves.Notifications;

/// <summary>
/// Demo handler for <see cref="LeaveRequestApproved"/>. The outbox dispatcher
/// publishes the event after the original transaction commits. Replace the log
/// with your real side effects (email payslip, push notification, calendar
/// event, etc.) — and add more handlers if multiple consumers care.
/// </summary>
internal sealed class LeaveRequestApprovedHandler(ILogger<LeaveRequestApprovedHandler> log)
    : INotificationHandler<LeaveRequestApproved>
{
    public Task Handle(LeaveRequestApproved e, CancellationToken ct)
    {
        log.LogInformation(
            "Leave request {RequestId} for employee {EmployeeId} approved by {ApproverId}",
            e.RequestId, e.EmployeeId, e.ApproverId);

        // TODO: queue an email/push notification, sync to a calendar, etc.
        return Task.CompletedTask;
    }
}
