using System.ComponentModel.DataAnnotations;

namespace KidsAttendance.Web.ViewModels;

public class ChildCreateViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre completo es requerido.")]
    [StringLength(150, ErrorMessage = "El nombre completo no puede tener más de 150 caracteres.")]
    [Display(Name = "Nombre completo")]
    public string FullName { get; set; } = string.Empty;

    [Display(Name = "Fecha de nacimiento")]
    [DataType(DataType.Date)]
    public DateTime? BirthDate { get; set; }

    [Range(0, 20, ErrorMessage = "La edad debe estar entre 0 y 20.")]
    [Display(Name = "Edad")]
    public int? Age { get; set; }

    [Required(ErrorMessage = "El grupo actual es requerido.")]
    [Range(1, int.MaxValue, ErrorMessage = "Seleccioná un grupo válido.")]
    [Display(Name = "Grupo actual")]
    public int CurrentClassGroupId { get; set; }

    [Display(Name = "Activo")]
    public bool IsActive { get; set; } = true;

    public List<int> SelectedGuardianIds { get; set; } = new();
}
