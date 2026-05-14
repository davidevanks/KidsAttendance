namespace KidsAttendance.Infrastructure.Persistence.Entities;

public class ChildGuardian
{
    public int Id { get; set; }
    public int ChildId { get; set; }
    public int GuardianId { get; set; }
    public string Relationship { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public bool IsAuthorizedPickup { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}
