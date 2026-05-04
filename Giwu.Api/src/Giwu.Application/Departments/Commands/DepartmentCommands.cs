using FluentValidation;
using Giwu.Application.Common;
using Giwu.Contracts.Departments;
using Giwu.Domain.Organization;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Giwu.Application.Departments.Commands;

// ── Create ──────────────────────────────────────────────────────────────────
public sealed record CreateDepartmentCommand(CreateDepartmentRequest Request)
    : IRequest<Result<DepartmentDto>>;

public sealed class CreateDepartmentValidator : AbstractValidator<CreateDepartmentCommand>
{
    public CreateDepartmentValidator()
    {
        RuleFor(x => x.Request.Name).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Request.Code).NotEmpty().MaximumLength(16);
    }
}

internal sealed class CreateDepartmentHandler(IApplicationDbContext db)
    : IRequestHandler<CreateDepartmentCommand, Result<DepartmentDto>>
{
    public async Task<Result<DepartmentDto>> Handle(CreateDepartmentCommand cmd, CancellationToken ct)
    {
        var r = cmd.Request;
        if (await db.Departments.AnyAsync(d => d.Code == r.Code, ct))
            return Result<DepartmentDto>.Conflict($"Department code '{r.Code}' already exists.");

        var dept = new Department
        {
            Name = r.Name, Code = r.Code,
            ParentDepartmentId = r.ParentDepartmentId, HeadEmployeeId = r.HeadEmployeeId,
            CostCenter = r.CostCenter, IsActive = true,
        };
        db.Departments.Add(dept);
        await db.SaveChangesAsync(ct);

        return Result<DepartmentDto>.Success(new DepartmentDto(
            dept.Id, dept.Name, dept.Code, dept.ParentDepartmentId, dept.HeadEmployeeId,
            dept.CostCenter, dept.IsActive, 0));
    }
}

// ── Update ──────────────────────────────────────────────────────────────────
public sealed record UpdateDepartmentCommand(Guid Id, UpdateDepartmentRequest Request)
    : IRequest<Result<DepartmentDto>>;

public sealed class UpdateDepartmentValidator : AbstractValidator<UpdateDepartmentCommand>
{
    public UpdateDepartmentValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Request.Name).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Request.Code).NotEmpty().MaximumLength(16);
    }
}

internal sealed class UpdateDepartmentHandler(IApplicationDbContext db)
    : IRequestHandler<UpdateDepartmentCommand, Result<DepartmentDto>>
{
    public async Task<Result<DepartmentDto>> Handle(UpdateDepartmentCommand cmd, CancellationToken ct)
    {
        var d = await db.Departments.FirstOrDefaultAsync(x => x.Id == cmd.Id, ct);
        if (d is null) return Result<DepartmentDto>.NotFound();

        var r = cmd.Request;
        if (r.Code != d.Code && await db.Departments.AnyAsync(x => x.Code == r.Code && x.Id != d.Id, ct))
            return Result<DepartmentDto>.Conflict($"Department code '{r.Code}' already exists.");

        d.Name = r.Name;
        d.Code = r.Code;
        d.ParentDepartmentId = r.ParentDepartmentId;
        d.HeadEmployeeId = r.HeadEmployeeId;
        d.CostCenter = r.CostCenter;
        d.IsActive = r.IsActive;

        await db.SaveChangesAsync(ct);

        var count = await db.Employees.CountAsync(e => e.DepartmentId == d.Id, ct);
        return Result<DepartmentDto>.Success(new DepartmentDto(
            d.Id, d.Name, d.Code, d.ParentDepartmentId, d.HeadEmployeeId,
            d.CostCenter, d.IsActive, count));
    }
}

// ── Delete (soft) ───────────────────────────────────────────────────────────
public sealed record DeleteDepartmentCommand(Guid Id) : IRequest<Result>;

internal sealed class DeleteDepartmentHandler(IApplicationDbContext db)
    : IRequestHandler<DeleteDepartmentCommand, Result>
{
    public async Task<Result> Handle(DeleteDepartmentCommand cmd, CancellationToken ct)
    {
        var d = await db.Departments.FirstOrDefaultAsync(x => x.Id == cmd.Id, ct);
        if (d is null) return Result.NotFound();

        if (await db.Employees.AnyAsync(e => e.DepartmentId == d.Id, ct))
            return Result.Conflict("Cannot delete a department with active employees. Reassign them first.");

        db.Departments.Remove(d);   // soft-delete via DbContext interceptor
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
