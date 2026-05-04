using FluentValidation;
using Giwu.Application.Common;
using Giwu.Contracts.Benefits;
using Giwu.Domain.Benefits;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Giwu.Application.Benefits.Programs;

public sealed record ListBenefitProgramsQuery(bool IncludeInactive, BenefitCategory? Category)
    : IRequest<Result<IReadOnlyList<BenefitProgramDto>>>;

internal sealed class ListBenefitProgramsHandler(IApplicationDbContext db)
    : IRequestHandler<ListBenefitProgramsQuery, Result<IReadOnlyList<BenefitProgramDto>>>
{
    public async Task<Result<IReadOnlyList<BenefitProgramDto>>> Handle(
        ListBenefitProgramsQuery q, CancellationToken ct)
    {
        var query = db.BenefitPrograms.AsQueryable();
        if (!q.IncludeInactive) query = query.Where(p => p.IsActive);
        if (q.Category.HasValue) query = query.Where(p => p.Category == q.Category.Value);

        var items = await query
            .OrderBy(p => p.Name)
            .Select(p => new BenefitProgramDto(
                p.Id, p.Name, p.Provider, p.Category, p.Description, p.IsActive, p.IsMandatory,
                p.Eligibility, p.EffectiveDate, p.RenewalDate,
                p.MonthlyCostPerEmployee, p.EmployerShare, p.EmployeeShare, p.ShareIsPercent,
                db.BenefitEnrollments.Count(en => en.BenefitProgramId == p.Id
                    && en.Status == EnrollmentStatus.Active)))
            .ToListAsync(ct);

        return Result<IReadOnlyList<BenefitProgramDto>>.Success(items);
    }
}

public sealed record GetBenefitProgramQuery(Guid Id) : IRequest<Result<BenefitProgramDto>>;

internal sealed class GetBenefitProgramHandler(IApplicationDbContext db)
    : IRequestHandler<GetBenefitProgramQuery, Result<BenefitProgramDto>>
{
    public async Task<Result<BenefitProgramDto>> Handle(GetBenefitProgramQuery q, CancellationToken ct)
    {
        var p = await db.BenefitPrograms.FirstOrDefaultAsync(x => x.Id == q.Id, ct);
        if (p is null) return Result<BenefitProgramDto>.NotFound();

        var count = await db.BenefitEnrollments.CountAsync(en =>
            en.BenefitProgramId == p.Id && en.Status == EnrollmentStatus.Active, ct);

        return Result<BenefitProgramDto>.Success(new BenefitProgramDto(
            p.Id, p.Name, p.Provider, p.Category, p.Description, p.IsActive, p.IsMandatory,
            p.Eligibility, p.EffectiveDate, p.RenewalDate,
            p.MonthlyCostPerEmployee, p.EmployerShare, p.EmployeeShare, p.ShareIsPercent, count));
    }
}

public sealed record CreateBenefitProgramCommand(CreateBenefitProgramRequest Request)
    : IRequest<Result<BenefitProgramDto>>;

public sealed class CreateBenefitProgramValidator : AbstractValidator<CreateBenefitProgramCommand>
{
    public CreateBenefitProgramValidator()
    {
        RuleFor(x => x.Request.Name).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Request.Provider).MaximumLength(128);
        RuleFor(x => x.Request.MonthlyCostPerEmployee).GreaterThanOrEqualTo(0);
    }
}

internal sealed class CreateBenefitProgramHandler(IApplicationDbContext db)
    : IRequestHandler<CreateBenefitProgramCommand, Result<BenefitProgramDto>>
{
    public async Task<Result<BenefitProgramDto>> Handle(CreateBenefitProgramCommand cmd, CancellationToken ct)
    {
        var r = cmd.Request;
        var p = new BenefitProgram
        {
            Name = r.Name, Provider = r.Provider, Category = r.Category, Description = r.Description,
            IsActive = true, IsMandatory = r.IsMandatory, Eligibility = r.Eligibility,
            EffectiveDate = r.EffectiveDate, RenewalDate = r.RenewalDate,
            MonthlyCostPerEmployee = r.MonthlyCostPerEmployee,
            EmployerShare = r.EmployerShare, EmployeeShare = r.EmployeeShare, ShareIsPercent = r.ShareIsPercent,
        };
        db.BenefitPrograms.Add(p);
        await db.SaveChangesAsync(ct);

        return Result<BenefitProgramDto>.Success(new BenefitProgramDto(
            p.Id, p.Name, p.Provider, p.Category, p.Description, p.IsActive, p.IsMandatory,
            p.Eligibility, p.EffectiveDate, p.RenewalDate,
            p.MonthlyCostPerEmployee, p.EmployerShare, p.EmployeeShare, p.ShareIsPercent, 0));
    }
}

public sealed record UpdateBenefitProgramCommand(Guid Id, UpdateBenefitProgramRequest Request)
    : IRequest<Result>;

internal sealed class UpdateBenefitProgramHandler(IApplicationDbContext db)
    : IRequestHandler<UpdateBenefitProgramCommand, Result>
{
    public async Task<Result> Handle(UpdateBenefitProgramCommand cmd, CancellationToken ct)
    {
        var p = await db.BenefitPrograms.FirstOrDefaultAsync(x => x.Id == cmd.Id, ct);
        if (p is null) return Result.NotFound();

        var r = cmd.Request;
        p.Name = r.Name; p.Provider = r.Provider; p.Category = r.Category;
        p.Description = r.Description; p.IsActive = r.IsActive; p.IsMandatory = r.IsMandatory;
        p.Eligibility = r.Eligibility; p.EffectiveDate = r.EffectiveDate; p.RenewalDate = r.RenewalDate;
        p.MonthlyCostPerEmployee = r.MonthlyCostPerEmployee;
        p.EmployerShare = r.EmployerShare; p.EmployeeShare = r.EmployeeShare;
        p.ShareIsPercent = r.ShareIsPercent;

        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public sealed record DeleteBenefitProgramCommand(Guid Id) : IRequest<Result>;

internal sealed class DeleteBenefitProgramHandler(IApplicationDbContext db)
    : IRequestHandler<DeleteBenefitProgramCommand, Result>
{
    public async Task<Result> Handle(DeleteBenefitProgramCommand cmd, CancellationToken ct)
    {
        var p = await db.BenefitPrograms.FirstOrDefaultAsync(x => x.Id == cmd.Id, ct);
        if (p is null) return Result.NotFound();
        if (await db.BenefitEnrollments.AnyAsync(en =>
                en.BenefitProgramId == p.Id && en.Status == EnrollmentStatus.Active, ct))
            return Result.Conflict("Cannot delete a program with active enrollments. Deactivate it instead.");

        db.BenefitPrograms.Remove(p);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
