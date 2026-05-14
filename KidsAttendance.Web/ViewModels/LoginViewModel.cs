using System.ComponentModel.DataAnnotations;

namespace KidsAttendance.Web.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "El correo es requerido.")]
    [EmailAddress(ErrorMessage = "El correo no es válido.")]
    [Display(Name = "Correo")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es requerida.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Recordarme")]
    public bool RememberMe { get; set; }
}
