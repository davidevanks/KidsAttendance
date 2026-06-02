namespace KidsAttendance.Web.ViewModels;

/// <summary>
/// Represents a guardian row in the index view, including assigned children for display.
/// </summary>
public class GuardianIndexViewModel
{
    public int Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public string? SecondaryPhoneNumber { get; set; }

    public bool IsActive { get; set; }

    public string ChildrenText { get; set; } = "-";
}
