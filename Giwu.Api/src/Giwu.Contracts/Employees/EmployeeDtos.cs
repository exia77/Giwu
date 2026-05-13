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
    DateOnly HireDate,
    string Phone,
    DateOnly? BirthDate,
    Gender Gender,
    decimal MonthlyBaseSalary,
    string AddressLine,
    string City,
    string Province);

public sealed record CreateEmployeeRequest(
    string EmployeeNumber,
    string FirstName,
    string LastName,
    string Email,
    string JobTitle,
    Guid DepartmentId,
    DateOnly HireDate,
    decimal MonthlyBaseSalary,
    EmploymentType EmploymentType = EmploymentType.Regular,
    string Phone = "",
    DateOnly? BirthDate = null,
    Gender Gender = Gender.PreferNotToSay,
    string AddressLine = "",
    string City = "",
    string Province = "");

public sealed record UpdateEmployeeRequest(
    string FirstName,
    string LastName,
    string Email,
    string JobTitle,
    Guid DepartmentId,
    decimal MonthlyBaseSalary,
    EmploymentStatus Status = EmploymentStatus.Active,
    EmploymentType EmploymentType = EmploymentType.Regular,
    DateOnly? HireDate = null,
    string Phone = "",
    DateOnly? BirthDate = null,
    Gender Gender = Gender.PreferNotToSay,
    string AddressLine = "",
    string City = "",
    string Province = "");
