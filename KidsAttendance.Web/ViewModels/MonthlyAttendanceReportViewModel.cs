namespace KidsAttendance.Web.ViewModels;

/// <summary>
/// Represents the monthly attendance matrix shown to a teacher for their assigned group.
/// </summary>
public class MonthlyAttendanceReportViewModel
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public bool HasAssignedGroup { get; set; }
    public IReadOnlyList<DateTime> AttendanceDates { get; set; } = new List<DateTime>();
    public IReadOnlyList<MonthlyAttendanceChildRowViewModel> Rows { get; set; } = new List<MonthlyAttendanceChildRowViewModel>();
}

/// <summary>
/// Represents one child row in the monthly attendance matrix.
/// </summary>
public class MonthlyAttendanceChildRowViewModel
{
    public string ChildName { get; set; } = string.Empty;
    public IReadOnlySet<DateTime> AttendedDates { get; set; } = new HashSet<DateTime>();
    public int TotalAttendance { get; set; }
}
