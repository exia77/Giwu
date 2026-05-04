namespace Giwu.Domain.Leaves;

public enum LeaveAccrualMode { Yearly, Monthly, PerEvent, Tenure }

public enum LeaveCategory
{
    Vacation, Sick, Emergency, Maternity, Paternity, SoloParent,
    Bereavement, Magna, Vawc, Sil, Birthday, Unpaid, Other,
}

public enum LeaveRequestStatus { Pending, Approved, Rejected, Cancelled, Taken }
