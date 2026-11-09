using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[Route("api/users")]
[Authorize]
public class UsersController : ApiControllerBase
{
    private readonly RaceDayDbContext _db;
    private readonly IBlobStorageService _blobs;

    public UsersController(RaceDayDbContext db, IBlobStorageService blobs)
    {
        _db = db;
        _blobs = blobs;
    }

    /// <summary>Gets the logged-in user's own account and profile details.</summary>
    /// <response code="200">The user, including their profile if one exists.</response>
    /// <response code="401">Not logged in.</response>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserResponse>> GetMe()
    {
        var id = CurrentUserId;
        var user = await _db.Users.AsNoTracking().Include(u => u.Profile).FirstOrDefaultAsync(u => u.UserID == id);
        if (user == null) return Unauthorized(new ErrorResponse("Account no longer exists."));
        return UserResponse.From(user);
    }

    /// <summary>Updates the logged-in user's name and phone number.</summary>
    /// <remarks>Body: fullName (required), phoneNumber (optional). Available to both roles.</remarks>
    /// <response code="200">The updated user.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    [HttpPut("me")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserResponse>> UpdateMe(UpdateUserRequest request)
    {
        var id = CurrentUserId;
        var user = await _db.Users.Include(u => u.Profile).FirstOrDefaultAsync(u => u.UserID == id);
        if (user == null) return Unauthorized(new ErrorResponse("Account no longer exists."));

        user.FullName = request.FullName.Trim();
        user.PhoneNumber = request.PhoneNumber;
        await _db.SaveChangesAsync();

        return UserResponse.From(user);
    }

    /// <summary>Creates or updates the logged-in user's extended profile.</summary>
    /// <remarks>
    /// Body: dateOfBirth, gender, emergencyContactName, emergencyContactPhone, medicalNotes (all optional).
    /// Available to both Organisers and Participants.
    /// </remarks>
    /// <response code="200">The saved profile.</response>
    /// <response code="400">Validation failed (e.g. date of birth in the future).</response>
    /// <response code="401">Not logged in.</response>
    [HttpPut("me/profile")]
    [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ProfileResponse>> UpdateMyProfile(ProfileRequest request)
    {
        if (request.DateOfBirth.HasValue && request.DateOfBirth.Value > DateOnly.FromDateTime(DateTime.Today))
            return BadRequest(new ErrorResponse("Date of birth cannot be in the future."));

        var id = CurrentUserId;
        var profile = await _db.UserProfiles.FirstOrDefaultAsync(p => p.UserID == id);
        if (profile == null)
        {
            profile = new UserProfile { UserID = id };
            _db.UserProfiles.Add(profile);
        }

        profile.DateOfBirth = request.DateOfBirth;
        profile.Gender = request.Gender;
        profile.EmergencyContactName = request.EmergencyContactName;
        profile.EmergencyContactPhone = request.EmergencyContactPhone;
        profile.MedicalNotes = request.MedicalNotes;
        await _db.SaveChangesAsync();

        return ProfileResponse.From(profile);
    }

    /// <summary>Gets a specific user's details (Organisers only).</summary>
    /// <response code="200">The user.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Caller is not an Organiser.</response>
    /// <response code="404">User does not exist.</response>
    [HttpGet("{id:int}")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> GetUser(int id)
    {
        var user = await _db.Users.AsNoTracking().Include(u => u.Profile).FirstOrDefaultAsync(u => u.UserID == id);
        if (user == null) return NotFound(new ErrorResponse("User not found."));
        return UserResponse.From(user);
    }

    /// <summary>Uploads or replaces the logged-in user's profile picture.</summary>
    /// <remarks>
    /// Send multipart/form-data with one file field named "file" (JPG, PNG or WebP, max 5 MB).
    /// The image is stored in Azure Blob Storage and its URL is saved on the profile as profilePictureUrl.
    /// Available to both roles.
    /// </remarks>
    /// <response code="200">The profile with its new profilePictureUrl.</response>
    /// <response code="400">No file, wrong type, too large, or not a real image.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="503">Image storage is unavailable.</response>
    [HttpPost("me/profile-picture")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ProfileResponse>> UploadProfilePicture(IFormFile file)
    {
        var (error, extension) = await ImageValidator.ValidateAsync(file);
        if (error != null) return BadRequest(new ErrorResponse(error));

        var id = CurrentUserId;
        var profile = await _db.UserProfiles.FirstOrDefaultAsync(p => p.UserID == id);
        if (profile == null)
        {
            profile = new UserProfile { UserID = id };
            _db.UserProfiles.Add(profile);
        }

        try
        {
            await using var stream = file.OpenReadStream();
            profile.ProfilePictureUrl = await _blobs.UploadImageAsync(stream, "profiles", extension, file.ContentType);
        }
        catch (BlobStorageUnavailableException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new ErrorResponse("Image storage is currently unavailable. Please try again later."));
        }

        await _db.SaveChangesAsync();
        return ProfileResponse.From(profile);
    }
}