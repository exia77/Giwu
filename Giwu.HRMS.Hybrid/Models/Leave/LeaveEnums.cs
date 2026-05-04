namespace Giwu.HRMS.Hybrid.Models.Leave;

public enum LeaveAccrualMode
{
    Yearly,         // granted in full at start of year
    Monthly,        // accrues each month (e.g. 1.25/mo for 15 yearly)
    PerEvent,       // one-time grant per qualifying event (maternity, bereavement)
    Tenure,         // granted after a tenure milestone (SIL = 1 year)
}

public enum LeaveCategory
{
    Vacation,
    Sick,
    Emergency,
    Maternity,
    Paternity,
    SoloParent,
    Bereavement,
    Magna,           // Magna Carta for Women
    Vawc,            // VAWC (RA 9262)
    Sil,             // Service Incentive Leave (Labor Code)
    Birthday,
    Unpaid,
    Other,
}

public enum LeaveRequestStatus
{
    Pending,
    Approved,
    Rejected,
    Cancelled,
    Taken,
}
