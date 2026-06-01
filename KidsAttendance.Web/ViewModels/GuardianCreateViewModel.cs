using System.ComponentModel.DataAnnotations;

namespace KidsAttendance.Web.ViewModels;

public class GuardianCreateViewModel
{
    private const string DigitsOnlyPhonePattern = @"^\d{8,15}$";

    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre completo es requerido.")]
    [StringLength(150, ErrorMessage = "El nombre completo no puede tener más de 150 caracteres.")]
    [Display(Name = "Nombre completo")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "El celular es requerido.")]
    [StringLength(30, ErrorMessage = "El celular no puede tener más de 30 caracteres.")]
    [RegularExpression(DigitsOnlyPhonePattern, ErrorMessage = "El celular debe contener solo números (8 a 15 dígitos).")]
    [Display(Name = "Celular")]
    public string PhoneNumber { get; set; } = string.Empty;

    [StringLength(30, ErrorMessage = "El teléfono secundario no puede tener más de 30 caracteres.")]
    [RegularExpression(DigitsOnlyPhonePattern, ErrorMessage = "El teléfono secundario debe contener solo números (8 a 15 dígitos).")]
    [Display(Name = "Teléfono secundario")]
    public string? SecondaryPhoneNumber { get; set; }

    [Display(Name = "Activo")]
    public bool IsActive { get; set; } = true;
}
