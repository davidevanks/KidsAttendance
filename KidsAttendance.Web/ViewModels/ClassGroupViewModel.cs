using System.ComponentModel.DataAnnotations;

namespace KidsAttendance.Web.ViewModels;

public class ClassGroupViewModel : IValidatableObject
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es requerido.")]
    [StringLength(100, ErrorMessage = "El nombre no puede tener más de 100 caracteres.")]
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

    public string AssignedTeachersText { get; set; } = "Sin maestros asignados";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (MinAge > MaxAge)
        {
            yield return new ValidationResult(
                "La edad mínima no puede ser mayor que la edad máxima.",
                new[] { nameof(MinAge), nameof(MaxAge) });
        }
    }
}
