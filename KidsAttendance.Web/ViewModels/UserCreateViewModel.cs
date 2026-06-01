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
    [RegularExpression("^[a-zA-Z0-9]+$", ErrorMessage = "La cuenta solo permite letras y números.")]
    [Display(Name = "Cuenta")]
    public string Account { get; set; } = string.Empty;

    [StringLength(30)]
    [Phone]
    [Display(Name = "Celular")]
    public string? PhoneNumber { get; set; }

    [Display(Name = "Activo")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Asistencia Global")]
    public bool AsistenciaGlobal { get; set; } = false;

    [Display(Name = "Rol")]
    public string Role { get; set; } = "Teacher";

    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string? Password { get; set; }
}
