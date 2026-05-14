using System.ComponentModel.DataAnnotations;

namespace KidsAttendance.Web.ViewModels;

public class AttendanceReportFilterViewModel
{
    [DataType(DataType.Date)]
    [Display(Name = "Fecha")]
    public DateTime Date { get; set; } = DateTime.Today;
}
