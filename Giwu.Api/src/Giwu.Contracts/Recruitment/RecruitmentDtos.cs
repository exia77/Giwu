using Giwu.Domain.Recruitment;

namespace Giwu.Contracts.Recruitment;

public sealed record JobRequisitionDto(
    Guid Id,
    string Title,
    Guid DepartmentId,
    string DepartmentName,
    string Location,
    string EmploymentType,
    int Openings,
    int Filled,
    JobStatus Status,
    Guid? OwnerEmployeeId,
    string? OwnerName,
    decimal SalaryMin,
    decimal SalaryMax,
    DateTimeOffset PostedAt,
    DateOnly? TargetFillBy,
    string Description,
    int ActiveCandidateCount);

public sealed record CreateJobRequisitionRequest(
    string Title,
    Guid DepartmentId,
    string Location,
    string EmploymentType,
    int Openings,
    Guid? OwnerEmployeeId,
    decimal SalaryMin,
    decimal SalaryMax,
    DateOnly? TargetFillBy,
    string Description,
    JobStatus Status = JobStatus.Draft);

public sealed record UpdateJobRequisitionRequest(
    string Title,
    Guid DepartmentId,
    string Location,
    string EmploymentType,
    int Openings,
    int Filled,
    Guid? OwnerEmployeeId,
    decimal SalaryMin,
    decimal SalaryMax,
    DateOnly? TargetFillBy,
    string Description);

public sealed record ChangeJobStatusRequest(JobStatus Status);

public sealed record CandidateDto(
    Guid Id,
    Guid JobRequisitionId,
    string JobTitle,
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    CandidateStage Stage,
    string Source,
    int Rating,
    DateTimeOffset AppliedAt,
    DateTimeOffset LastActivityAt,
    string Notes,
    string? RejectionReason);

public sealed record CreateCandidateRequest(
    Guid JobRequisitionId,
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    string Source,
    int Rating,
    string Notes);

public sealed record UpdateCandidateRequest(
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    string Source,
    int Rating,
    string Notes);

public sealed record AdvanceCandidateRequest(CandidateStage NewStage, string? Note);

public sealed record RejectCandidateRequest(string Reason);

public sealed record InterviewDto(
    Guid Id,
    Guid CandidateId,
    string CandidateName,
    DateTimeOffset ScheduledAt,
    int DurationMinutes,
    InterviewKind Kind,
    Guid? InterviewerEmployeeId,
    string? InterviewerName,
    string Location,
    InterviewStatus Status,
    string Notes);

public sealed record ScheduleInterviewRequest(
    Guid CandidateId,
    DateTimeOffset ScheduledAt,
    int DurationMinutes,
    InterviewKind Kind,
    Guid? InterviewerEmployeeId,
    string Location,
    string Notes);

public sealed record UpdateInterviewRequest(
    DateTimeOffset ScheduledAt,
    int DurationMinutes,
    InterviewKind Kind,
    Guid? InterviewerEmployeeId,
    string Location,
    InterviewStatus Status,
    string Notes);
