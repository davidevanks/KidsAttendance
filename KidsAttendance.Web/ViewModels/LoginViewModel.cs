using System.ComponentModel.DataAnnotations;

namespace KidsAttendance.Web.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "La cuenta es requerida.")]
    [RegularExpression("^[a-zA-Z0-9]+$", ErrorMessage = "La cuenta solo permite letras y números.")]
    [Display(Name = "Cuenta")]
    public string Account { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es requerida.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Recordarme")]
    public bool RememberMe { get; set; }
}
