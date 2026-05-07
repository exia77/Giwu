using FluentValidation;
using Giwu.Application.Common;
using Giwu.Contracts.Settings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Giwu.Application.Settings;

// ── Get ─────────────────────────────────────────────────────────────────────
public sealed record GetOrganizationProfileQuery : IRequest<Result<OrganizationProfileDto>>;

internal sealed class GetOrganizationProfileHandler(IApplicationDbContext db, ICurrentUser user)
    : IRequestHandler<GetOrganizationProfileQuery, Result<OrganizationProfileDto>>
{
    public async Task<Result<OrganizationProfileDto>> Handle(GetOrganizationProfileQuery _, CancellationToken ct)
    {
        if (!user.IsAuthenticated) return Result<OrganizationProfileDto>.Forbidden();

        var t = await db.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == user.TenantId && x.DeletedAt == null, ct);

        if (t is null) return Result<OrganizationProfileDto>.NotFound();

        return Result<OrganizationProfileDto>.Success(new OrganizationProfileDto(
            t.LegalName, t.TradeName,
            t.Address, t.City, t.Province, t.PostalCode,
            t.Phone, t.Email, t.Website,
            t.Tin, t.RdoCode,
            t.SssEmployerNumber, t.PhilHealthEmployerNumber, t.PagibigEmployerNumber,
            t.DoleEstablishmentNumber));
    }
}

// ── Update ──────────────────────────────────────────────────────────────────
public sealed record UpdateOrganizationProfileCommand(UpdateOrganizationProfileRequest Request)
    : IRequest<Result<OrganizationProfileDto>>;

public sealed class UpdateOrganizationProfileValidator : AbstractValidator<UpdateOrganizationProfileCommand>
{
    public UpdateOrganizationProfileValidator()
    {
        RuleFor(x => x.Request.LegalName).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Request.TradeName).MaximumLength(256);
        RuleFor(x => x.Request.Address).MaximumLength(512);
        RuleFor(x => x.Request.City).MaximumLength(128);
        RuleFor(x => x.Request.Province).MaximumLength(128);
        RuleFor(x => x.Request.PostalCode).MaximumLength(16);
        RuleFor(x => x.Request.Phone).MaximumLength(64);
        RuleFor(x => x.Request.Email).MaximumLength(256)
            .EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Request.Email));
        RuleFor(x => x.Request.Website).MaximumLength(256);
        RuleFor(x => x.Request.Tin).MaximumLength(32);
        RuleFor(x => x.Request.RdoCode).MaximumLength(8);
        RuleFor(x => x.Request.SssEmployerNo).MaximumLength(32);
        RuleFor(x => x.Request.PhilHealthEmployerNo).MaximumLength(32);
        RuleFor(x => x.Request.PagibigEmployerNo).MaximumLength(32);
        RuleFor(x => x.Request.DoleEstablishmentNo).MaximumLength(64);
    }
}

internal sealed class UpdateOrganizationProfileHandler(IApplicationDbContext db, ICurrentUser user)
    : IRequestHandler<UpdateOrganizationProfileCommand, Result<OrganizationProfileDto>>
{
    public async Task<Result<OrganizationProfileDto>> Handle(UpdateOrganizationProfileCommand cmd, CancellationToken ct)
    {
        if (!user.IsAuthenticated) return Result<OrganizationProfileDto>.Forbidden();

        var t = await db.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == user.TenantId && x.DeletedAt == null, ct);

        if (t is null) return Result<OrganizationProfileDto>.NotFound();

        var r = cmd.Request;
        t.LegalName               = r.LegalName;
        t.TradeName               = r.TradeName;
        t.Address                 = r.Address;
        t.City                    = r.City;
        t.Province                = r.Province;
        t.PostalCode              = r.PostalCode;
        t.Phone                   = r.Phone;
        t.Email                   = r.Email;
        t.Website                 = r.Website;
        t.Tin                     = r.Tin;
        t.RdoCode                 = r.RdoCode;
        t.SssEmployerNumber       = r.SssEmployerNo;
        t.PhilHealthEmployerNumber= r.PhilHealthEmployerNo;
        t.PagibigEmployerNumber   = r.PagibigEmployerNo;
        t.DoleEstablishmentNumber = r.DoleEstablishmentNo;

        await db.SaveChangesAsync(ct);

        return Result<OrganizationProfileDto>.Success(new OrganizationProfileDto(
            t.LegalName, t.TradeName,
            t.Address, t.City, t.Province, t.PostalCode,
            t.Phone, t.Email, t.Website,
            t.Tin, t.RdoCode,
            t.SssEmployerNumber, t.PhilHealthEmployerNumber, t.PagibigEmployerNumber,
            t.DoleEstablishmentNumber));
    }
}
