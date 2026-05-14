namespace KidsAttendance.Infrastructure.Persistence.Entities;

public class ChildGroupHistory
{
    public int Id { get; set; }
    public int ChildId { get; set; }
    public int? FromClassGroupId { get; set; }
    public int ToClassGroupId { get; set; }
    public string ChangedByUserId { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
    public string? Reason { get; set; }
}
