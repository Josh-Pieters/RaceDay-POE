namespace RaceDay.Api.Models;

/// <summary>Maps to the "Users" table. Named AppUser to avoid clashing with ControllerBase.User.</summary>
public class AppUser
{
    public int UserID { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public UserProfile? Profile { get; set; }
    public ICollection<Event> OrganisedEvents { get; set; } = new List<Event>();
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}