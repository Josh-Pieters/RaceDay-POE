using Microsoft.AspNetCore.Http;
using RaceDay.Web.Models;
using RaceDay.Web.Models.Api;

namespace RaceDay.Web.Services;

/// <summary>Every call the web app makes to the RaceDay API.</summary>
public interface IApiClient
{
    // Authentication
    Task<ApiResponse<LoginResult>> LoginAsync(string email, string password);
    Task<ApiResponse<ApiUser>> RegisterAsync(RegisterViewModel model);
    Task<ApiResponse> LogoutAsync();

    // Profile
    Task<ApiResponse<ApiUser>> GetMeAsync();
    Task<ApiResponse<ApiUser>> UpdateMeAsync(string fullName, string? phoneNumber);
    Task<ApiResponse<ApiProfile>> UpdateProfileAsync(ProfileViewModel model);
    Task<ApiResponse<ApiProfile>> UploadProfilePictureAsync(IFormFile file);

    // Events
    Task<ApiResponse<List<ApiEvent>>> GetEventsAsync(EventFilter filter);
    Task<ApiResponse<ApiEvent>> GetEventAsync(int id);
    Task<ApiResponse<List<ApiEvent>>> GetMyEventsAsync();
    Task<ApiResponse<ApiEvent>> CreateEventAsync(EventFormViewModel form);
    Task<ApiResponse<ApiEvent>> UpdateEventAsync(int id, EventFormViewModel form);
    Task<ApiResponse> DeleteEventAsync(int id);
    Task<ApiResponse<ApiEvent>> UploadBannerAsync(int eventId, IFormFile file);
    Task<ApiResponse<List<ApiEnrolment>>> GetEventEnrolmentsAsync(int eventId);

    // Categories
    Task<ApiResponse<ApiCategory>> CreateCategoryAsync(int eventId, CategoryFormViewModel form);
    Task<ApiResponse<ApiCategory>> UpdateCategoryAsync(int categoryId, CategoryFormViewModel form);
    Task<ApiResponse> DeleteCategoryAsync(int categoryId);

    // Enrolments
    Task<ApiResponse<ApiEnrolment>> EnrolAsync(int categoryId);
    Task<ApiResponse<List<ApiEnrolment>>> GetMyEnrolmentsAsync();
    Task<ApiResponse> CancelEnrolmentAsync(int enrolmentId);

    // Results
    Task<ApiResponse<ApiRaceResult>> CaptureResultAsync(int enrolmentId, ResultFormViewModel form);
    Task<ApiResponse<ApiRaceResult>> UpdateResultAsync(int resultId, ResultFormViewModel form);
    Task<ApiResponse<List<ApiRaceResult>>> GetMyResultsAsync();
}