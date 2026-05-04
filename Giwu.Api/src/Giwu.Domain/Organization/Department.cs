using Giwu.Domain.Common;

namespace Giwu.Domain.Organization;

public class Department : AuditableEntity
{
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
    public Guid? ParentDepartmentId { get; set; }
    public Guid? HeadEmployeeId { get; set; }
    public string CostCenter { get; set; } = "";
    public bool IsActive { get; set; } = true;
}
