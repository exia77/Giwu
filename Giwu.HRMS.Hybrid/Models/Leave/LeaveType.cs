namespace Giwu.HRMS.Hybrid.Models.Leave;

/// <summary>
/// Catalog entry describing a leave type the company offers (Vacation, Sick, etc).
/// API shape — also used directly by the UI when rendering the leave-type catalog.
/// </summary>
public class LeaveType
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";          // short code: VL, SL, EL, ML, PL
    public LeaveCategory Category { get; set; }
    public string Description { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public bool IsPaid { get; set; } = true;
    public bool IsMandated { get; set; }            // PH-law-required leave types

    // Entitlement
    public decimal AnnualEntitlementDays { get; set; }
    public LeaveAccrualMode AccrualMode { get; set; }
    public bool CarryoverAllowed { get; set; }
    public decimal MaxCarryoverDays { get; set; }
    public bool RequiresMedCert { get; set; }       // typical for SL > 2 days
    public int RequiresMedCertAfterDays { get; set; } = 2;
    public string Eligibility { get; set; } = "";   // free-form
    public string LegalReference { get; set; } = ""; // e.g. "Labor Code Art. 95"
}
