namespace KidsAttendance.Infrastructure.Persistence.Entities;

public class TeacherClassGroup
{
    public int Id { get; set; }
    public string TeacherUserId { get; set; } = string.Empty;
    public int ClassGroupId { get; set; }
    public DateTime AssignedAt { get; set; }
    public string? AssignedByUserId { get; set; }
    public bool IsActive { get; set; } = true;
}
