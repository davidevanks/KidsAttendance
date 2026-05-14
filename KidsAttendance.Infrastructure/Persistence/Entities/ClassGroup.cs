namespace KidsAttendance.Infrastructure.Persistence.Entities;

public class ClassGroup
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int MinAge { get; set; }
    public int MaxAge { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
