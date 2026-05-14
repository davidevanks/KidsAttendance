namespace KidsAttendance.Infrastructure.Persistence.Entities;

public class AttendanceRecord
{
    public int Id { get; set; }
    public int AttendanceSessionId { get; set; }
    public int ChildId { get; set; }
    public int ClassGroupId { get; set; }
    public string? TokenNumber { get; set; }
    public int CheckInGuardianId { get; set; }
    public int? CheckOutGuardianId { get; set; }
    public DateTime CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public byte[]? CheckInSignatureData { get; set; }
    public string? CheckInSignaturePath { get; set; }
    public string? CheckOutSignaturePath { get; set; }
    public string CheckInTeacherId { get; set; } = string.Empty;
    public string? CheckOutTeacherId { get; set; }
    public string Status { get; set; } = "CheckedIn";
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
