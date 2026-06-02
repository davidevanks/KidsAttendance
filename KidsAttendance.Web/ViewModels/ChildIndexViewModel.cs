namespace KidsAttendance.Web.ViewModels;

/// <summary>
/// Represents a child row in the index view, including assigned guardians and their phone numbers.
/// </summary>
public class ChildIndexViewModel
{
    public int Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public int? Age { get; set; }

    public int CurrentClassGroupId { get; set; }

    public bool IsActive { get; set; }

    public string GuardiansText { get; set; } = "-";

    public string GuardianPhoneNumbersText { get; set; } = "-";
}
