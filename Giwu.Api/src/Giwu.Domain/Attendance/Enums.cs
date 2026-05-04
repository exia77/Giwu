namespace Giwu.Domain.Attendance;

public enum AttendanceSource { App, Biometric, Web, Manual, Geofence }
public enum AttendanceStatus { Present, Late, Absent, OnLeave, Holiday, RestDay, Suspended }
public enum OvertimeType { Regular, RestDay, RegularHoliday, SpecialHoliday, NightDifferential }
public enum ApprovalStatus { Pending, Approved, Rejected, Cancelled }
