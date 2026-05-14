using System.ComponentModel.DataAnnotations;

namespace KidsAttendance.Web.ViewModels;

public class UserCreateViewModel
{
    public string? Id { get; set; }

    [Required]
    [StringLength(150)]
    [Display(Name = "Nombre completo")]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [Display(Name = "Correo")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Activo")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Rol")]
    public string Role { get; set; } = "Teacher";

    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string? Password { get; set; }
}
