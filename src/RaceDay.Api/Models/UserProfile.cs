namespace RaceDay.Api.Models;

public class UserProfile
{
    public int ProfileID { get; set; }
    public int UserID { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? MedicalNotes { get; set; }

    public AppUser User { get; set; } = null!;
}
