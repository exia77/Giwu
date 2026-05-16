namespace Giwu.Domain.Tenancy;

/// <summary>
/// Which paid plan a tenant is on. Numeric value is the rank — higher = more
/// features. The tier-limits service keys off this enum.
/// </summary>
public enum SubscriptionTier
{
    Basic      = 0, // up to 10 employees, core HR only
    Pro        = 1, // up to 50 employees, recruitment + custom reports
    Enterprise = 2, // unlimited, SSO + advanced features
}

/// <summary>
/// Lifecycle state of a tenant's subscription independent of tier.
/// Suspended/Cancelled tenants get read-only access; PastDue keeps working
/// during a grace window before downgrading.
/// </summary>
public enum SubscriptionStatus
{
    Trial      = 0,
    Active     = 1,
    PastDue    = 2,
    Cancelled  = 3,
    Suspended  = 4,
}
