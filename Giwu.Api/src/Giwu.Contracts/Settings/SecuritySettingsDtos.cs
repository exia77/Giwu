using Giwu.Domain.Tenancy;

namespace Giwu.Contracts.Settings;

public sealed record SecuritySettingsDto(
    bool RequireMfa,
    int MinPasswordLength,
    bool RequireUppercase,
    bool RequireLowercase,
    bool RequireNumber,
    bool RequireSpecial,
    int PasswordExpiryDays,
    SessionTimeout SessionTimeout,
    int MaxFailedLoginAttempts,
    bool IpWhitelistEnabled,
    string IpWhitelist);

public sealed record UpdateSecuritySettingsRequest(
    bool RequireMfa,
    int MinPasswordLength,
    bool RequireUppercase,
    bool RequireLowercase,
    bool RequireNumber,
    bool RequireSpecial,
    int PasswordExpiryDays,
    SessionTimeout SessionTimeout,
    int MaxFailedLoginAttempts,
    bool IpWhitelistEnabled,
    string IpWhitelist);
