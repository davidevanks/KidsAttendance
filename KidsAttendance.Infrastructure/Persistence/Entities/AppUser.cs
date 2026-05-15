using Microsoft.AspNetCore.Identity;

namespace KidsAttendance.Infrastructure.Persistence.Entities;

public class AppUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool AsistenciaGlobal { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
