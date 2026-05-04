using Giwu.Domain.Common;

namespace Giwu.Domain.Attendance;

public class Shift : AuditableEntity
{
    public string Name { get; set; } = "";
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int BreakMinutes { get; set; }
    public bool[] WorkDays { get; set; } = new bool[7];   // Mon..Sun
    public int GraceMinutes { get; set; }
}

public class EmployeeShiftAssignment : AuditableEntity
{
    public Guid EmployeeId { get; set; }
    public Guid ShiftId { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
}
