namespace KidsAttendance.Infrastructure.Persistence.Entities;

public class AttendanceSession
{
    public int Id { get; set; }
    public DateTime SessionDate { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsOpen { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public string? CreatedByUserId { get; set; }
}
