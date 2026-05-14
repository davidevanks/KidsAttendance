using System.ComponentModel.DataAnnotations;

namespace KidsAttendance.Web.ViewModels;

public class ChildCreateViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(150)]
    [Display(Name = "Nombre completo")]
    public string FullName { get; set; } = string.Empty;

    [Display(Name = "Fecha de nacimiento")]
    [DataType(DataType.Date)]
    public DateTime? BirthDate { get; set; }

    [Range(0, 20)]
    [Display(Name = "Edad")]
    public int? Age { get; set; }

    [Required]
    [Display(Name = "Grupo actual")]
    public int CurrentClassGroupId { get; set; }

    [Display(Name = "Activo")]
    public bool IsActive { get; set; } = true;

    public List<int> SelectedGuardianIds { get; set; } = new();
}
