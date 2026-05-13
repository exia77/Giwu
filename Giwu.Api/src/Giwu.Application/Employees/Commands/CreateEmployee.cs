using FluentValidation;
using Giwu.Application.Common;
using Giwu.Contracts.Employees;
using Giwu.Domain.Common;
using Giwu.Domain.Organization;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Giwu.Application.Employees.Commands;

public sealed record CreateEmployeeCommand(CreateEmployeeRequest Request)
    : IRequest<Result<EmployeeDto>>;

public sealed class CreateEmployeeValidator : AbstractValidator<CreateEmployeeCommand>
{
    public CreateEmployeeValidator()
    {
        RuleFor(x => x.Request.EmployeeNumber).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Request.FirstName).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Request.LastName).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Request.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Request.JobTitle).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Request.DepartmentId).NotEmpty();
        RuleFor(x => x.Request.MonthlyBaseSalary).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Request.EmploymentType).IsInEnum();
        RuleFor(x => x.Request.Gender).IsInEnum();
        RuleFor(x => x.Request.Phone).MaximumLength(32);
        RuleFor(x => x.Request.AddressLine).MaximumLength(256);
        RuleFor(x => x.Request.City).MaximumLength(128);
        RuleFor(x => x.Request.Province).MaximumLength(128);
    }
}

internal sealed class CreateEmployeeHandler(IApplicationDbContext db)
    : IRequestHandler<CreateEmployeeCommand, Result<EmployeeDto>>
{
    public async Task<Result<EmployeeDto>> Handle(CreateEmployeeCommand cmd, CancellationToken ct)
    {
        var r = cmd.Request;

        var dept = await db.Departments.FirstOrDefaultAsync(d => d.Id == r.DepartmentId, ct);
        if (dept is null) return Result<EmployeeDto>.NotFound("Department not found");

        if (await db.Employees.AnyAsync(e => e.EmployeeNumber == r.EmployeeNumber, ct))
            return Result<EmployeeDto>.Forbidden("Employee number already taken");

        var emp = new Employee
        {
            EmployeeNumber    = r.EmployeeNumber,
            FirstName         = r.FirstName,
            LastName          = r.LastName,
            Email             = r.Email,
            JobTitle          = r.JobTitle,
            DepartmentId      = r.DepartmentId,
            HireDate          = r.HireDate,
            MonthlyBaseSalary = r.MonthlyBaseSalary,
            EmploymentType    = r.EmploymentType,
            Phone             = r.Phone,
            BirthDate         = r.BirthDate,
            Gender            = r.Gender,
            PermanentAddress  = new Address
            {
                Line1    = r.AddressLine,
                City     = r.City,
                Province = r.Province,
            },
        };

        db.Employees.Add(emp);
        await db.SaveChangesAsync(ct);

        return Result<EmployeeDto>.Success(new EmployeeDto(
            emp.Id, emp.EmployeeNumber, emp.FirstName, emp.LastName, emp.Email,
            emp.JobTitle, emp.DepartmentId, dept.Name,
            emp.Status, emp.EmploymentType, emp.HireDate,
            emp.Phone, emp.BirthDate, emp.Gender, emp.MonthlyBaseSalary,
            emp.PermanentAddress.Line1, emp.PermanentAddress.City, emp.PermanentAddress.Province));
    }
}
