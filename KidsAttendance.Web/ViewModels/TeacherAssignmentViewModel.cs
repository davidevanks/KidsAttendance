using System.ComponentModel.DataAnnotations;

namespace KidsAttendance.Web.ViewModels;

public class TeacherAssignmentViewModel
{
    [Required(ErrorMessage = "Seleccioná un maestro.")]
    [Display(Name = "Maestro")]
    public string TeacherUserId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Seleccioná un grupo.")]
    [Range(1, int.MaxValue, ErrorMessage = "Seleccioná un grupo válido.")]
    [Display(Name = "Grupo")]
    public int ClassGroupId { get; set; }
}
