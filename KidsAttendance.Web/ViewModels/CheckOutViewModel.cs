using System.ComponentModel.DataAnnotations;

namespace KidsAttendance.Web.ViewModels;

public class CheckOutViewModel
{
    [Required]
    public List<int> RecordIds { get; set; } = new();

    [Required]
    public int CheckOutGuardianId { get; set; }
    public string? CheckOutSignatureBase64 { get; set; }
}
