using System.ComponentModel.DataAnnotations;
using RaceDay.Web.Models.Api;

namespace RaceDay.Web.Models;

public static class Lookups
{
    public static readonly string[] Provinces =
    {
        "Eastern Cape", "Free State", "Gauteng", "KwaZulu-Natal", "Limpopo",
        "Mpumalanga", "Northern Cape", "North West", "Western Cape"
    };

    public static readonly string[] EventTypes = { "Run", "Walk", "Cycle" };
    public static readonly string[] Genders = { "Female", "Male", "Other" };

    public const string Confirmed = "Confirmed";
    public const string Organiser = "Organiser";
    public const string Participant = "Participant";
}

// ------------------------------------------------------------------ Account

public class LoginViewModel
{
    [Required(ErrorMessage = "Please enter your email address.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your password.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Please enter your full name.")]
    [StringLength(100)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your email address.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [StringLength(20)]
    [Display(Name = "Phone number")]
    public string? PhoneNumber { get; set; }

    [Required(ErrorMessage = "Please choose a password.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Your password must be at least 8 characters.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm your password.")]
    [Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = Lookups.Participant;

    [Display(Name = "Organiser code")]
    public string? OrganiserCode { get; set; }
}

// ------------------------------------------------------------------ Events

public class EventFilter
{
    public string? Search { get; set; }
    public string? Province { get; set; }
    public string? EventType { get; set; }
    public DateOnly? FromDate { get; set; }
}

public class HomeViewModel
{
    public EventFilter Filter { get; set; } = new();
    public List<ApiEvent> Events { get; set; } = new();
}

public class EventDetailsViewModel
{
    public ApiEvent Event { get; set; } = new();
    public ApiEnrolment? MyEnrolment { get; set; }
}

public class EventFormViewModel
{
    public int? EventID { get; set; }

    [Required(ErrorMessage = "Please enter the event name.")]
    [StringLength(150)]
    [Display(Name = "Event name")]
    public string EventName { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Please choose the event date.")]
    [Display(Name = "Date")]
    public DateOnly? EventDate { get; set; }

    [Display(Name = "Start time")]
    public TimeOnly? StartTime { get; set; }

    [Required(ErrorMessage = "Please enter the location.")]
    [StringLength(200)]
    public string Location { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please choose a province.")]
    public string? Province { get; set; }

    [Required(ErrorMessage = "Please choose an event type.")]
    [Display(Name = "Event type")]
    public string? EventType { get; set; }

    [Required(ErrorMessage = "Please enter the distance in km.")]
    [Range(0.1, 1000, ErrorMessage = "Distance must be between 0.1 and 1000 km.")]
    [Display(Name = "Distance (km)")]
    public decimal? DistanceKM { get; set; }

    [Display(Name = "Banner image")]
    public IFormFile? Banner { get; set; }

    public string? CurrentBannerUrl { get; set; }
}

public class EventPickerViewModel
{
    public string Title { get; set; } = string.Empty;
    public string ControllerName { get; set; } = string.Empty;
    public List<ApiEvent> Events { get; set; } = new();
    public string? Error { get; set; }
}

public record EventTabsModel(int EventId, string EventName, string Active);

// ------------------------------------------------------------------ Organiser

public class OrganiserDashboardViewModel
{
    public List<ApiEvent> Events { get; set; } = new();
    public int TotalEvents { get; set; }
    public int UpcomingCount { get; set; }
    public int TotalEnrolments { get; set; }
    public ApiEvent? NextEvent { get; set; }
}

public class CategoryFormViewModel
{
    public int? CategoryID { get; set; }
    public int EventID { get; set; }

    [Required(ErrorMessage = "Please enter a category name, e.g. Under 20 or 21 km.")]
    [StringLength(50)]
    [Display(Name = "Category name")]
    public string CategoryName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter the distance in km.")]
    [Range(0.1, 1000, ErrorMessage = "Distance must be between 0.1 and 1000 km.")]
    [Display(Name = "Distance (km)")]
    public decimal? DistanceKM { get; set; }

    [Required(ErrorMessage = "Please enter the entry price (0 for free).")]
    [Range(0, 100000, ErrorMessage = "Price must be between 0 and 100 000.")]
    [Display(Name = "Price (R)")]
    public decimal? Price { get; set; }

    [Required(ErrorMessage = "Please enter the maximum number of participants.")]
    [Range(1, 1000000, ErrorMessage = "Maximum participants must be at least 1.")]
    [Display(Name = "Max participants")]
    public int? MaxParticipants { get; set; }
}

public class CategoriesViewModel
{
    public ApiEvent Event { get; set; } = new();
    public CategoryFormViewModel NewCategory { get; set; } = new();
}

public class EventEnrolmentsViewModel
{
    public ApiEvent Event { get; set; } = new();
    public List<ApiEnrolment> Enrolments { get; set; } = new();
}

public class ResultFormViewModel
{
    public int EventID { get; set; }
    public int EnrolmentID { get; set; }
    public int? ResultID { get; set; }

    [Required(ErrorMessage = "Please enter a finish time.")]
    [Display(Name = "Finish time")]
    public TimeOnly? FinishTime { get; set; }

    [Required(ErrorMessage = "Please enter the overall position.")]
    [Range(1, int.MaxValue, ErrorMessage = "Position must be 1 or higher.")]
    [Display(Name = "Overall position")]
    public int? OverallPosition { get; set; }

    [Required(ErrorMessage = "Please enter the category position.")]
    [Range(1, int.MaxValue, ErrorMessage = "Position must be 1 or higher.")]
    [Display(Name = "Category position")]
    public int? CategoryPosition { get; set; }
}

// ------------------------------------------------------------------ Profile / errors

public class ProfileViewModel
{
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your full name.")]
    [StringLength(100)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [StringLength(20)]
    [Display(Name = "Phone number")]
    public string? PhoneNumber { get; set; }

    [Display(Name = "Date of birth")]
    public DateOnly? DateOfBirth { get; set; }

    [StringLength(10)]
    public string? Gender { get; set; }

    [StringLength(100)]
    [Display(Name = "Emergency contact name")]
    public string? EmergencyContactName { get; set; }

    [StringLength(20)]
    [Display(Name = "Emergency contact phone")]
    public string? EmergencyContactPhone { get; set; }

    [StringLength(500)]
    [Display(Name = "Medical notes")]
    public string? MedicalNotes { get; set; }

    public string? ProfilePictureUrl { get; set; }
}
public class ErrorViewModel
{
    public int Code { get; set; }
    public string Title { get; set; } = "Something went wrong";
    public string Message { get; set; } = "Please try again. If the problem continues, come back a little later.";

    public string? RequestId { get; set; }
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
