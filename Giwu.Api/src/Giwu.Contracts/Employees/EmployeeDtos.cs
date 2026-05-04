using Giwu.Domain.Organization;

namespace Giwu.Contracts.Employees;

public sealed record EmployeeDto(
    Guid Id,
    string EmployeeNumber,
    string FirstName,
    string LastName,
    string Email,
    string JobTitle,
    Guid DepartmentId,
    string DepartmentName,
    EmploymentStatus Status,
    EmploymentType EmploymentType,
    DateOnly HireDate);

public sealed record CreateEmployeeRequest(
    string EmployeeNumber,
    string FirstName,
    string LastName,
    string Email,
    string JobTitle,
    Guid DepartmentId,
    DateOnly HireDate,
    decimal MonthlyBaseSalary,
    EmploymentType EmploymentType = EmploymentType.Regular);

public sealed record UpdateEmployeeRequest(
    string FirstName,
    string LastName,
    string Email,
    string JobTitle,
    Guid DepartmentId,
    decimal MonthlyBaseSalary);
