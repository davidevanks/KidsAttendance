using System.ComponentModel.DataAnnotations;

namespace KidsAttendance.Web.ViewModels;

public class CheckOutViewModel
{
    [Required(ErrorMessage = "Seleccioná al menos un niño para registrar salida.")]
    [MinLength(1, ErrorMessage = "Seleccioná al menos un niño para registrar salida.")]
    public List<int> RecordIds { get; set; } = new();

    [Required(ErrorMessage = "Seleccioná el padre que retira.")]
    [Range(1, int.MaxValue, ErrorMessage = "Seleccioná un padre válido.")]
    public int CheckOutGuardianId { get; set; }

    [Required(ErrorMessage = "La firma de salida es requerida.")]
    public string? CheckOutSignatureBase64 { get; set; }
}
