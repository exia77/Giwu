using Giwu.Domain.Common;

namespace Giwu.Domain.Recruitment;

public class Interview : AuditableEntity
{
    public Guid CandidateId { get; set; }
    public DateTimeOffset ScheduledAt { get; set; }
    public int DurationMinutes { get; set; } = 60;
    public InterviewKind Kind { get; set; } = InterviewKind.PhoneScreen;
    public Guid? InterviewerEmployeeId { get; set; }
    public string Location { get; set; } = "";
    public InterviewStatus Status { get; set; } = InterviewStatus.Scheduled;
    public string Notes { get; set; } = "";
}
