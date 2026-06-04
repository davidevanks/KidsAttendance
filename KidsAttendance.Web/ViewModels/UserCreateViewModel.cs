using KidsAttendance.Infrastructure.Security;
using System.ComponentModel.DataAnnotations;

namespace KidsAttendance.Web.ViewModels;

public class UserCreateViewModel
{
    private const string DigitsOnlyPhonePattern = @"^\d{8,15}$";

    public string? Id { get; set; }

    [Required(ErrorMessage = "El nombre completo es requerido.")]
    [StringLength(150, ErrorMessage = "El nombre completo no puede tener más de 150 caracteres.")]
    [Display(Name = "Nombre completo")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "La cuenta es requerida.")]
    [RegularExpression("^[a-zA-Z0-9]+$", ErrorMessage = "La cuenta solo permite letras y números.")]
    [Display(Name = "Cuenta")]
    public string Account { get; set; } = string.Empty;

    [StringLength(30, ErrorMessage = "El celular no puede tener más de 30 caracteres.")]
    [RegularExpression(DigitsOnlyPhonePattern, ErrorMessage = "El celular debe contener solo números (8 a 15 dígitos).")]
    [Display(Name = "Celular")]
    public string? PhoneNumber { get; set; }

    [Display(Name = "Activo")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Asistencia Global")]
    public bool AsistenciaGlobal { get; set; } = false;

    [Display(Name = "Rol")]
    public string Role { get; set; } = ApplicationRoles.Teacher;

    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string? Password { get; set; }
}
