using FluentValidation;
using Giwu.Application.Common;
using Giwu.Contracts.Employees;
using Giwu.Domain.Organization;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Giwu.Application.Employees.Commands;

public sealed record UpdateEmployeeCommand(Guid Id, UpdateEmployeeRequest Request)
    : IRequest<Result<EmployeeDto>>;

public sealed class UpdateEmployeeValidator : AbstractValidator<UpdateEmployeeCommand>
{
    public UpdateEmployeeValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Request.FirstName).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Request.LastName).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Request.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Request.JobTitle).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Request.DepartmentId).NotEmpty();
        RuleFor(x => x.Request.MonthlyBaseSalary).GreaterThanOrEqualTo(0);
    }
}

internal sealed class UpdateEmployeeHandler(IApplicationDbContext db)
    : IRequestHandler<UpdateEmployeeCommand, Result<EmployeeDto>>
{
    public async Task<Result<EmployeeDto>> Handle(UpdateEmployeeCommand cmd, CancellationToken ct)
    {
        var emp = await db.Employees.FirstOrDefaultAsync(e => e.Id == cmd.Id, ct);
        if (emp is null) return Result<EmployeeDto>.NotFound();

        var dept = await db.Departments.FirstOrDefaultAsync(d => d.Id == cmd.Request.DepartmentId, ct);
        if (dept is null) return Result<EmployeeDto>.NotFound("Department not found");

        var r = cmd.Request;
        emp.FirstName         = r.FirstName;
        emp.LastName          = r.LastName;
        emp.Email             = r.Email;
        emp.JobTitle          = r.JobTitle;
        emp.DepartmentId      = r.DepartmentId;
        emp.MonthlyBaseSalary = r.MonthlyBaseSalary;

        await db.SaveChangesAsync(ct);

        return Result<EmployeeDto>.Success(new EmployeeDto(
            emp.Id, emp.EmployeeNumber, emp.FirstName, emp.LastName, emp.Email,
            emp.JobTitle, emp.DepartmentId, dept.Name,
            emp.Status, emp.EmploymentType, emp.HireDate));
    }
}

// ── Soft delete ─────────────────────────────────────────────────────────────
public sealed record DeleteEmployeeCommand(Guid Id) : IRequest<Result>;

internal sealed class DeleteEmployeeHandler(IApplicationDbContext db, TimeProvider clock)
    : IRequestHandler<DeleteEmployeeCommand, Result>
{
    public async Task<Result> Handle(DeleteEmployeeCommand cmd, CancellationToken ct)
    {
        var emp = await db.Employees.FirstOrDefaultAsync(e => e.Id == cmd.Id, ct);
        if (emp is null) return Result.NotFound();

        emp.Status = EmploymentStatus.Terminated;
        emp.SeparationDate = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);

        db.Employees.Remove(emp);   // soft-delete via DbContext interceptor
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
