namespace Giwu.Domain.Recruitment;

public enum JobStatus
{
    Draft,
    Open,
    OnHold,
    Closed,
}

public enum CandidateStage
{
    Applied,
    Screening,
    Interview,
    Offer,
    Hired,
    Rejected,
}

public enum InterviewKind
{
    PhoneScreen,
    Technical,
    Panel,
    Final,
    Other,
}

public enum InterviewStatus
{
    Scheduled,
    Completed,
    Cancelled,
    NoShow,
}
