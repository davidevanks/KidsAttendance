using System.ComponentModel.DataAnnotations;

namespace KidsAttendance.Web.ViewModels;

public class TeacherAssignmentViewModel
{
    [Required]
    [Display(Name = "Maestro")]
    public string TeacherUserId { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Grupo")]
    public int ClassGroupId { get; set; }
}
