using System.Security.Claims;
using Giwu.Application.Common;

namespace Giwu.Api.Common;

public sealed class CurrentUserService(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public Guid Id => GetGuid("sub");
    public Guid TenantId => GetGuid("tid");
    public Guid? EmployeeId
    {
        get
        {
            var v = Principal?.FindFirstValue("eid");
            return Guid.TryParse(v, out var g) ? g : null;
        }
    }

    public string Email => Principal?.FindFirstValue("email") ?? "";
    public string DisplayName => Principal?.FindFirstValue("name") ?? "";

    public IReadOnlyCollection<string> Roles =>
        Principal?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray() ?? Array.Empty<string>();

    public IReadOnlyCollection<string> Permissions =>
        Principal?.FindAll("perm").Select(c => c.Value).ToArray() ?? Array.Empty<string>();

    private Guid GetGuid(string claim)
    {
        var v = Principal?.FindFirstValue(claim);
        return Guid.TryParse(v, out var g) ? g : Guid.Empty;
    }
}
