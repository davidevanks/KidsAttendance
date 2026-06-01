using System.ComponentModel.DataAnnotations;

namespace KidsAttendance.Web.ViewModels;

public class CheckInViewModel
{
    [Required(ErrorMessage = "Seleccioná un padre.")]
    [Range(1, int.MaxValue, ErrorMessage = "Seleccioná un padre válido.")]
    public int GuardianId { get; set; }
    [Required(ErrorMessage = "Seleccioná un grupo.")]
    [Range(1, int.MaxValue, ErrorMessage = "Seleccioná un grupo válido.")]
    public int ClassGroupId { get; set; }
    [Required(ErrorMessage = "Seleccioná al menos un niño.")]
    [MinLength(1, ErrorMessage = "Seleccioná al menos un niño.")]
    public List<int> ChildIds { get; set; } = new();

    [StringLength(30, ErrorMessage = "La ficha no puede tener más de 30 caracteres.")]
    public string? TokenNumber { get; set; }

    public string? CheckInSignatureBase64 { get; set; }
}
