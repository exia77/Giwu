using Giwu.Domain.Common;

namespace Giwu.Domain.Recruitment;

public class Candidate : AuditableEntity
{
    public Guid JobRequisitionId { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public CandidateStage Stage { get; set; } = CandidateStage.Applied;
    public string Source { get; set; } = "";
    public int Rating { get; set; }
    public DateTimeOffset AppliedAt { get; set; }
    public DateTimeOffset LastActivityAt { get; set; }
    public string Notes { get; set; } = "";
    public string? RejectionReason { get; set; }
}
