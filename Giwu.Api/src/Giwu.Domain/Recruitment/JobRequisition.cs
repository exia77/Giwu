using Giwu.Domain.Common;

namespace Giwu.Domain.Recruitment;

public class JobRequisition : AuditableEntity
{
    public string Title { get; set; } = "";
    public Guid DepartmentId { get; set; }
    public string Location { get; set; } = "";
    public string EmploymentType { get; set; } = "Full-time";
    public int Openings { get; set; } = 1;
    public int Filled { get; set; }
    public JobStatus Status { get; set; } = JobStatus.Draft;
    public Guid? OwnerEmployeeId { get; set; }
    public decimal SalaryMin { get; set; }
    public decimal SalaryMax { get; set; }
    public DateTimeOffset PostedAt { get; set; }
    public DateOnly? TargetFillBy { get; set; }
    public string Description { get; set; } = "";
}
