namespace KidsAttendance.Infrastructure.Persistence.Entities;

public class Child
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public DateTime? BirthDate { get; set; }
    public int? Age { get; set; }
    public int CurrentClassGroupId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
