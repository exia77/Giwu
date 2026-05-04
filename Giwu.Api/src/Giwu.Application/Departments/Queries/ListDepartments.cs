using Giwu.Application.Common;
using Giwu.Contracts.Departments;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Giwu.Application.Departments.Queries;

public sealed record ListDepartmentsQuery(bool IncludeInactive = false)
    : IRequest<Result<IReadOnlyList<DepartmentDto>>>;

internal sealed class ListDepartmentsHandler(IApplicationDbContext db)
    : IRequestHandler<ListDepartmentsQuery, Result<IReadOnlyList<DepartmentDto>>>
{
    public async Task<Result<IReadOnlyList<DepartmentDto>>> Handle(
        ListDepartmentsQuery q, CancellationToken ct)
    {
        var query = db.Departments.AsQueryable();
        if (!q.IncludeInactive) query = query.Where(d => d.IsActive);

        var items = await query
            .OrderBy(d => d.Name)
            .Select(d => new DepartmentDto(
                d.Id, d.Name, d.Code, d.ParentDepartmentId, d.HeadEmployeeId,
                d.CostCenter, d.IsActive,
                db.Employees.Count(e => e.DepartmentId == d.Id)))
            .ToListAsync(ct);

        return Result<IReadOnlyList<DepartmentDto>>.Success(items);
    }
}

public sealed record GetDepartmentQuery(Guid Id) : IRequest<Result<DepartmentDto>>;

internal sealed class GetDepartmentHandler(IApplicationDbContext db)
    : IRequestHandler<GetDepartmentQuery, Result<DepartmentDto>>
{
    public async Task<Result<DepartmentDto>> Handle(GetDepartmentQuery q, CancellationToken ct)
    {
        var d = await db.Departments.FirstOrDefaultAsync(x => x.Id == q.Id, ct);
        if (d is null) return Result<DepartmentDto>.NotFound();

        var count = await db.Employees.CountAsync(e => e.DepartmentId == d.Id, ct);
        return Result<DepartmentDto>.Success(new DepartmentDto(
            d.Id, d.Name, d.Code, d.ParentDepartmentId, d.HeadEmployeeId,
            d.CostCenter, d.IsActive, count));
    }
}
