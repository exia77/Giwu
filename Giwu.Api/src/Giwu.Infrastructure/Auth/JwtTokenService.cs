using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Giwu.Application.Common;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Giwu.Infrastructure.Auth;

public sealed class JwtOptions
{
    public string Issuer   { get; set; } = "giwu-hrms";
    public string Audience { get; set; } = "giwu-hrms-clients";
    public string Key      { get; set; } = "";   // 32+ chars, set via env / user-secrets
    public int AccessTokenMinutes { get; set; } = 15;
}

public sealed class JwtTokenService(IOptions<JwtOptions> opts, TimeProvider clock)
    : IJwtTokenService
{
    private readonly JwtOptions _o = opts.Value;

    public (string AccessToken, DateTimeOffset ExpiresAt) IssueAccessToken(
        Guid userId, Guid tenantId, Guid? employeeId, string email, string displayName,
        IEnumerable<string> roles, IEnumerable<string> permissions)
    {
        var expiresAt = clock.GetUtcNow().AddMinutes(_o.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new("tid", tenantId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new("name", displayName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };
        if (employeeId.HasValue) claims.Add(new("eid", employeeId.Value.ToString()));
        foreach (var r in roles) claims.Add(new(ClaimTypes.Role, r));
        foreach (var p in permissions) claims.Add(new("perm", p));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_o.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer:   _o.Issuer,
            audience: _o.Audience,
            claims:   claims,
            notBefore: clock.GetUtcNow().UtcDateTime,
            expires:   expiresAt.UtcDateTime,
            signingCredentials: creds);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    public string IssueRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    public string HashRefreshToken(string token) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
