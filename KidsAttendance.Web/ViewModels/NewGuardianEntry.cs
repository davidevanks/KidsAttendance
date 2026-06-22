using System.ComponentModel.DataAnnotations;

namespace KidsAttendance.Web.ViewModels;

public class NewGuardianEntry
{
    private const string DigitsOnlyPhonePattern = @"^\d{8,15}$";

    [Required(ErrorMessage = "El nombre del tutor es requerido.")]
    [StringLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "El celular del tutor es requerido.")]
    [StringLength(30)]
    [RegularExpression(DigitsOnlyPhonePattern, ErrorMessage = "El celular debe tener entre 8 y 15 dígitos.")]
    public string PhoneNumber { get; set; } = string.Empty;

    [StringLength(30)]
    [RegularExpression(DigitsOnlyPhonePattern, ErrorMessage = "El celular secundario debe tener entre 8 y 15 dígitos.")]
    public string? SecondaryPhoneNumber { get; set; }

    [Required]
    [StringLength(50)]
    public string Relationship { get; set; } = "Padre";
}
