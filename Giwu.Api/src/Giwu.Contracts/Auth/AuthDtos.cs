namespace Giwu.Contracts.Auth;

public sealed record LoginRequest(string Email, string Password, string? TenantSlug = null);

public sealed record LoginResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt,
    UserMeDto User);

public sealed record RefreshRequest(string RefreshToken);

public sealed record UserMeDto(
    Guid Id,
    Guid TenantId,
    Guid? EmployeeId,
    string Email,
    string DisplayName,
    string[] Roles,
    string[] Permissions);
