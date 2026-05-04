using Giwu.Application.Common;
using Giwu.Contracts.Tenancy;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Giwu.Application.Tenancy.Queries;

public sealed record GetCurrentTenantQuery : IRequest<Result<TenantDto>>;

internal sealed class GetCurrentTenantHandler(IApplicationDbContext db, ICurrentUser user)
    : IRequestHandler<GetCurrentTenantQuery, Result<TenantDto>>
{
    public async Task<Result<TenantDto>> Handle(GetCurrentTenantQuery _, CancellationToken ct)
    {
        if (!user.IsAuthenticated) return Result<TenantDto>.Forbidden();

        var t = await db.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == user.TenantId && x.DeletedAt == null, ct);

        return t is null
            ? Result<TenantDto>.NotFound()
            : Result<TenantDto>.Success(new TenantDto(
                t.Id, t.Name, t.LegalName, t.Tin,
                t.DefaultCurrency, t.DefaultTimeZone, t.IsActive));
    }
}
