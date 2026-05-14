namespace KidsAttendance.Web.ViewModels;

public class AttendanceTodayRowViewModel
{
    public string GuardianName { get; set; } = string.Empty;
    public string GuardianPhone { get; set; } = string.Empty;
    public string ChildName { get; set; } = string.Empty;
    public string TokenNumber { get; set; } = string.Empty;
    public string DisplayStatus { get; set; } = string.Empty;
    public string? SignaturePath { get; set; }
}
