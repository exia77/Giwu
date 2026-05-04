namespace Giwu.Contracts.Departments;

public sealed record DepartmentDto(
    Guid Id,
    string Name,
    string Code,
    Guid? ParentDepartmentId,
    Guid? HeadEmployeeId,
    string CostCenter,
    bool IsActive,
    int EmployeeCount);

public sealed record CreateDepartmentRequest(
    string Name,
    string Code,
    Guid? ParentDepartmentId = null,
    Guid? HeadEmployeeId = null,
    string CostCenter = "");

public sealed record UpdateDepartmentRequest(
    string Name,
    string Code,
    Guid? ParentDepartmentId,
    Guid? HeadEmployeeId,
    string CostCenter,
    bool IsActive);
