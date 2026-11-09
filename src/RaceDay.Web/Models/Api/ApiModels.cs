namespace RaceDay.Web.Models.Api;

// These classes match the JSON the Part 2 API returns. They hold data only - no business rules.

public class ApiProfile
{
    public int ProfileID { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? MedicalNotes { get; set; }
    public string? ProfilePictureUrl { get; set; }
}

public class ApiUser
{
    public int UserID { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public ApiProfile? Profile { get; set; }
}

public class ApiCategory
{
    public int CategoryID { get; set; }
    public int EventID { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal DistanceKM { get; set; }
    public decimal Price { get; set; }
    public int MaxParticipants { get; set; }
}

public class ApiEvent
{
    public int EventID { get; set; }
    public int OrganiserID { get; set; }
    public string OrganiserName { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly EventDate { get; set; }
    public TimeOnly? StartTime { get; set; }
    public string Location { get; set; } = string.Empty;
    public string? Province { get; set; }
    public string EventType { get; set; } = string.Empty;
    public decimal DistanceKM { get; set; }
    public string? BannerImageUrl { get; set; }
    public int? EnrolmentCount { get; set; }
    public List<ApiCategory>? Categories { get; set; }
}

public class ApiResultSummary
{
    public int ResultID { get; set; }
    public TimeOnly? FinishTime { get; set; }
    public int? OverallPosition { get; set; }
    public int? CategoryPosition { get; set; }
}

public class ApiEnrolment
{
    public int EnrolmentID { get; set; }
    public int ParticipantID { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public int CategoryID { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int EventID { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateOnly EventDate { get; set; }
    public DateTime EnrolmentDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public ApiResultSummary? Result { get; set; }
}

public class ApiRaceResult
{
    public int ResultID { get; set; }
    public int EnrolmentID { get; set; }
    public int ParticipantID { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public int EventID { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateOnly EventDate { get; set; }
    public int CategoryID { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public TimeOnly? FinishTime { get; set; }
    public int? OverallPosition { get; set; }
    public int? CategoryPosition { get; set; }
    public int? TotalFinishers { get; set; }
}

/// <summary>What a successful login gives us: the user, and the API's session cookie value.</summary>
public record LoginResult(ApiUser User, string SessionCookie);