using System.ComponentModel.DataAnnotations;

namespace KidsAttendance.Web.ViewModels;

public class CheckInViewModel
{
    [Required]
    public int GuardianId { get; set; }
    [Required]
    public int ClassGroupId { get; set; }
    [Required]
    public List<int> ChildIds { get; set; } = new();

    [StringLength(30)]
    public string? TokenNumber { get; set; }

    public string? CheckInSignatureBase64 { get; set; }
}
