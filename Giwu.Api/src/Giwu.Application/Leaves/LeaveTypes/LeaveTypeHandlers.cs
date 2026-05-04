using FluentValidation;
using Giwu.Application.Common;
using Giwu.Contracts.Leaves;
using Giwu.Domain.Leaves;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Giwu.Application.Leaves.LeaveTypes;

// ── Queries ─────────────────────────────────────────────────────────────────
public sealed record ListLeaveTypesQuery(bool IncludeInactive = false)
    : IRequest<Result<IReadOnlyList<LeaveTypeDto>>>;

internal sealed class ListLeaveTypesHandler(IApplicationDbContext db)
    : IRequestHandler<ListLeaveTypesQuery, Result<IReadOnlyList<LeaveTypeDto>>>
{
    public async Task<Result<IReadOnlyList<LeaveTypeDto>>> Handle(
        ListLeaveTypesQuery q, CancellationToken ct)
    {
        var query = db.LeaveTypes.AsQueryable();
        if (!q.IncludeInactive) query = query.Where(t => t.IsActive);

        var items = await query
            .OrderBy(t => t.Name)
            .Select(t => Map(t))
            .ToListAsync(ct);

        return Result<IReadOnlyList<LeaveTypeDto>>.Success(items);
    }

    internal static LeaveTypeDto Map(LeaveType t) => new(
        t.Id, t.Name, t.Code, t.Category, t.Description, t.IsActive, t.IsPaid, t.IsMandated,
        t.AnnualEntitlementDays, t.AccrualMode, t.CarryoverAllowed, t.MaxCarryoverDays,
        t.RequiresMedCert, t.RequiresMedCertAfterDays, t.LegalReference);
}

// ── Create ──────────────────────────────────────────────────────────────────
public sealed record CreateLeaveTypeCommand(CreateLeaveTypeRequest Request)
    : IRequest<Result<LeaveTypeDto>>;

public sealed class CreateLeaveTypeValidator : AbstractValidator<CreateLeaveTypeCommand>
{
    public CreateLeaveTypeValidator()
    {
        RuleFor(x => x.Request.Name).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Request.Code).NotEmpty().MaximumLength(8);
        RuleFor(x => x.Request.AnnualEntitlementDays).GreaterThanOrEqualTo(0);
    }
}

internal sealed class CreateLeaveTypeHandler(IApplicationDbContext db)
    : IRequestHandler<CreateLeaveTypeCommand, Result<LeaveTypeDto>>
{
    public async Task<Result<LeaveTypeDto>> Handle(CreateLeaveTypeCommand cmd, CancellationToken ct)
    {
        var r = cmd.Request;
        if (await db.LeaveTypes.AnyAsync(t => t.Code == r.Code, ct))
            return Result<LeaveTypeDto>.Conflict($"Leave type code '{r.Code}' already exists.");

        var t = new LeaveType
        {
            Name = r.Name, Code = r.Code, Category = r.Category, Description = r.Description,
            IsActive = true, IsPaid = r.IsPaid, IsMandated = r.IsMandated,
            AnnualEntitlementDays = r.AnnualEntitlementDays, AccrualMode = r.AccrualMode,
            CarryoverAllowed = r.CarryoverAllowed, MaxCarryoverDays = r.MaxCarryoverDays,
            RequiresMedCert = r.RequiresMedCert, RequiresMedCertAfterDays = r.RequiresMedCertAfterDays,
            LegalReference = r.LegalReference,
        };
        db.LeaveTypes.Add(t);
        await db.SaveChangesAsync(ct);
        return Result<LeaveTypeDto>.Success(ListLeaveTypesHandler.Map(t));
    }
}

// ── Update ──────────────────────────────────────────────────────────────────
public sealed record UpdateLeaveTypeCommand(Guid Id, UpdateLeaveTypeRequest Request)
    : IRequest<Result<LeaveTypeDto>>;

internal sealed class UpdateLeaveTypeHandler(IApplicationDbContext db)
    : IRequestHandler<UpdateLeaveTypeCommand, Result<LeaveTypeDto>>
{
    public async Task<Result<LeaveTypeDto>> Handle(UpdateLeaveTypeCommand cmd, CancellationToken ct)
    {
        var t = await db.LeaveTypes.FirstOrDefaultAsync(x => x.Id == cmd.Id, ct);
        if (t is null) return Result<LeaveTypeDto>.NotFound();

        var r = cmd.Request;
        if (r.Code != t.Code && await db.LeaveTypes.AnyAsync(x => x.Code == r.Code && x.Id != t.Id, ct))
            return Result<LeaveTypeDto>.Conflict($"Leave type code '{r.Code}' already exists.");

        t.Name = r.Name; t.Code = r.Code; t.Category = r.Category; t.Description = r.Description;
        t.IsActive = r.IsActive; t.IsPaid = r.IsPaid; t.IsMandated = r.IsMandated;
        t.AnnualEntitlementDays = r.AnnualEntitlementDays; t.AccrualMode = r.AccrualMode;
        t.CarryoverAllowed = r.CarryoverAllowed; t.MaxCarryoverDays = r.MaxCarryoverDays;
        t.RequiresMedCert = r.RequiresMedCert; t.RequiresMedCertAfterDays = r.RequiresMedCertAfterDays;
        t.LegalReference = r.LegalReference;

        await db.SaveChangesAsync(ct);
        return Result<LeaveTypeDto>.Success(ListLeaveTypesHandler.Map(t));
    }
}

// ── Delete (soft) ───────────────────────────────────────────────────────────
public sealed record DeleteLeaveTypeCommand(Guid Id) : IRequest<Result>;

internal sealed class DeleteLeaveTypeHandler(IApplicationDbContext db)
    : IRequestHandler<DeleteLeaveTypeCommand, Result>
{
    public async Task<Result> Handle(DeleteLeaveTypeCommand cmd, CancellationToken ct)
    {
        var t = await db.LeaveTypes.FirstOrDefaultAsync(x => x.Id == cmd.Id, ct);
        if (t is null) return Result.NotFound();

        if (await db.LeaveRequests.AnyAsync(r => r.LeaveTypeId == t.Id, ct))
        {
            // Don't break history — just deactivate.
            t.IsActive = false;
            await db.SaveChangesAsync(ct);
            return Result.Success();
        }

        db.LeaveTypes.Remove(t);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
