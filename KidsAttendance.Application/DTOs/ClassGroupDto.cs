namespace KidsAttendance.Application.DTOs;

public class ClassGroupDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int MinAge { get; set; }
    public int MaxAge { get; set; }
    public bool IsActive { get; set; }
}
