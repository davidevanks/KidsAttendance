using System.ComponentModel.DataAnnotations;

namespace KidsAttendance.Web.ViewModels;

public class ClassGroupViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es requerido.")]
    [StringLength(100)]
    [Display(Name = "Nombre del grupo")]
    public string Name { get; set; } = string.Empty;

    [Range(0, 99, ErrorMessage = "Edad mínima no válida.")]
    [Display(Name = "Edad mínima")]
    public int MinAge { get; set; }

    [Range(0, 99, ErrorMessage = "Edad máxima no válida.")]
    [Display(Name = "Edad máxima")]
    public int MaxAge { get; set; }

    [Display(Name = "Activo")]
    public bool IsActive { get; set; } = true;
}
