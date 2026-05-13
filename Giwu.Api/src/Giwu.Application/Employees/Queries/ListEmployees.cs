using Giwu.Application.Common;
using Giwu.Contracts.Common;
using Giwu.Contracts.Employees;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Giwu.Application.Employees.Queries;

public sealed record ListEmployeesQuery(int Page = 1, int PageSize = 25, string? Search = null)
    : IRequest<Result<PagedResult<EmployeeDto>>>;

internal sealed class ListEmployeesHandler(IApplicationDbContext db)
    : IRequestHandler<ListEmployeesQuery, Result<PagedResult<EmployeeDto>>>
{
    public async Task<Result<PagedResult<EmployeeDto>>> Handle(
        ListEmployeesQuery q, CancellationToken ct)
    {
        var page = Math.Max(1, q.Page);
        var size = Math.Clamp(q.PageSize, 1, 200);

        var query = from e in db.Employees
                    join d in db.Departments on e.DepartmentId equals d.Id
                    select new { e, DeptName = d.Name };

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.Trim().ToLower();
            query = query.Where(x =>
                x.e.FirstName.ToLower().Contains(s) ||
                x.e.LastName.ToLower().Contains(s) ||
                x.e.Email.ToLower().Contains(s) ||
                x.e.EmployeeNumber.ToLower().Contains(s));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(x => x.e.LastName).ThenBy(x => x.e.FirstName)
            .Skip((page - 1) * size).Take(size)
            .Select(x => new EmployeeDto(
                x.e.Id, x.e.EmployeeNumber, x.e.FirstName, x.e.LastName, x.e.Email,
                x.e.JobTitle, x.e.DepartmentId, x.DeptName,
                x.e.Status, x.e.EmploymentType, x.e.HireDate,
                x.e.Phone, x.e.BirthDate, x.e.Gender, x.e.MonthlyBaseSalary,
                x.e.PermanentAddress.Line1, x.e.PermanentAddress.City, x.e.PermanentAddress.Province))
            .ToListAsync(ct);

        return Result<PagedResult<EmployeeDto>>.Success(
            new PagedResult<EmployeeDto>(items, total, page, size));
    }
}
