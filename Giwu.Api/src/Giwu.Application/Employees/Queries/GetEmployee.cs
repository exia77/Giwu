using Giwu.Application.Common;
using Giwu.Contracts.Employees;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Giwu.Application.Employees.Queries;

public sealed record GetEmployeeQuery(Guid Id) : IRequest<Result<EmployeeDto>>;

internal sealed class GetEmployeeHandler(IApplicationDbContext db)
    : IRequestHandler<GetEmployeeQuery, Result<EmployeeDto>>
{
    public async Task<Result<EmployeeDto>> Handle(GetEmployeeQuery q, CancellationToken ct)
    {
        var row = await (
            from e in db.Employees
            join d in db.Departments on e.DepartmentId equals d.Id
            where e.Id == q.Id
            select new { e, DeptName = d.Name }
        ).FirstOrDefaultAsync(ct);

        if (row is null) return Result<EmployeeDto>.NotFound();

        return Result<EmployeeDto>.Success(new EmployeeDto(
            row.e.Id, row.e.EmployeeNumber, row.e.FirstName, row.e.LastName, row.e.Email,
            row.e.JobTitle, row.e.DepartmentId, row.DeptName,
            row.e.Status, row.e.EmploymentType, row.e.HireDate,
            row.e.Phone, row.e.BirthDate, row.e.Gender, row.e.MonthlyBaseSalary,
            row.e.PermanentAddress.Line1, row.e.PermanentAddress.City, row.e.PermanentAddress.Province));
    }
}
