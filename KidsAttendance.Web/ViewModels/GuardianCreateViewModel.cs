using System.ComponentModel.DataAnnotations;

namespace KidsAttendance.Web.ViewModels;

public class GuardianCreateViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(150)]
    [Display(Name = "Nombre completo")]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [StringLength(30)]
    [Display(Name = "Celular")]
    public string PhoneNumber { get; set; } = string.Empty;

    [StringLength(30)]
    [Display(Name = "Teléfono secundario")]
    public string? SecondaryPhoneNumber { get; set; }

    [Display(Name = "Activo")]
    public bool IsActive { get; set; } = true;
}
