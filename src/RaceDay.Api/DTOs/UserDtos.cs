using System.ComponentModel.DataAnnotations;
using RaceDay.Api.Models;

namespace RaceDay.Api.DTOs;

public class UpdateUserRequest
{
    [Required, StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [StringLength(20)]
    public string? PhoneNumber { get; set; }
}

public class ProfileRequest
{
    public DateOnly? DateOfBirth { get; set; }

    [StringLength(10)]
    public string? Gender { get; set; }

    [StringLength(100)]
    public string? EmergencyContactName { get; set; }

    [StringLength(20)]
    public string? EmergencyContactPhone { get; set; }

    [StringLength(500)]
    public string? MedicalNotes { get; set; }
}

public class ProfileResponse
{
    public int ProfileID { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? MedicalNotes { get; set; }

    public static ProfileResponse From(UserProfile p) => new()
    {
        ProfileID = p.ProfileID,
        DateOfBirth = p.DateOfBirth,
        Gender = p.Gender,
        EmergencyContactName = p.EmergencyContactName,
        EmergencyContactPhone = p.EmergencyContactPhone,
        MedicalNotes = p.MedicalNotes
    };
}

public class UserResponse
{
    public int UserID { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public ProfileResponse? Profile { get; set; }

    public static UserResponse From(AppUser u) => new()
    {
        UserID = u.UserID,
        FullName = u.FullName,
        Email = u.Email,
        Role = u.Role,
        PhoneNumber = u.PhoneNumber,
        Profile = u.Profile == null ? null : ProfileResponse.From(u.Profile)
    };
}
