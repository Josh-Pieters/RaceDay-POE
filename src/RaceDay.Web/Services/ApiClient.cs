using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using RaceDay.Web.Models;
using RaceDay.Web.Models.Api;

namespace RaceDay.Web.Services;

public class ApiClient : IApiClient
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _accessor;
    private readonly ILogger<ApiClient> _logger;

    public ApiClient(HttpClient http, IHttpContextAccessor accessor, ILogger<ApiClient> logger)
    {
        _http = http;
        _accessor = accessor;
        _logger = logger;
    }

    // ------------------------------------------------------------------ Authentication

    public async Task<ApiResponse<LoginResult>> LoginAsync(string email, string password)
    {
        using var request = BuildRequest(HttpMethod.Post, "api/auth/login", JsonBody(new { email, password }));
        try
        {
            using var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var message = response.StatusCode == HttpStatusCode.Unauthorized
                    ? "Invalid email or password."
                    : await ReadErrorAsync(response);
                return ApiResponse<LoginResult>.Fail(message, response.StatusCode);
            }

            var user = await response.Content.ReadFromJsonAsync<ApiUser>(Json);
            var session = ReadSessionCookie(response);
            if (user == null || session == null)
                return ApiResponse<LoginResult>.Fail("We could not log you in. Please try again.", HttpStatusCode.BadGateway);

            return ApiResponse<LoginResult>.Ok(new LoginResult(user, session), response.StatusCode);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return Unavailable<LoginResult>(ex);
        }
    }

    public Task<ApiResponse<ApiUser>> RegisterAsync(RegisterViewModel model) =>
        SendAsync<ApiUser>(HttpMethod.Post, "api/auth/register", JsonBody(new
        {
            model.FullName,
            model.Email,
            model.Password,
            model.Role,
            model.PhoneNumber,
            model.OrganiserCode
        }));

    public async Task<ApiResponse> LogoutAsync() =>
        await SendAsync<object>(HttpMethod.Post, "api/auth/logout");

    // ------------------------------------------------------------------ Profile

    public Task<ApiResponse<ApiUser>> GetMeAsync() =>
        SendAsync<ApiUser>(HttpMethod.Get, "api/users/me");

    public Task<ApiResponse<ApiUser>> UpdateMeAsync(string fullName, string? phoneNumber) =>
        SendAsync<ApiUser>(HttpMethod.Put, "api/users/me", JsonBody(new { fullName, phoneNumber }));

    public Task<ApiResponse<ApiProfile>> UpdateProfileAsync(ProfileViewModel m) =>
        SendAsync<ApiProfile>(HttpMethod.Put, "api/users/me/profile", JsonBody(new
        {
            m.DateOfBirth,
            m.Gender,
            m.EmergencyContactName,
            m.EmergencyContactPhone,
            m.MedicalNotes
        }));

    public Task<ApiResponse<ApiProfile>> UploadProfilePictureAsync(IFormFile file) =>
        SendAsync<ApiProfile>(HttpMethod.Post, "api/users/me/profile-picture", FileBody(file));

    // ------------------------------------------------------------------ Events

    public Task<ApiResponse<List<ApiEvent>>> GetEventsAsync(EventFilter filter)
    {
        var query = new List<string>();
        void Add(string key, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value)) query.Add($"{key}={Uri.EscapeDataString(value)}");
        }
        Add("search", filter.Search);
        Add("province", filter.Province);
        Add("eventType", filter.EventType);
        Add("fromDate", filter.FromDate?.ToString("yyyy-MM-dd"));

        var url = "api/events" + (query.Count > 0 ? "?" + string.Join("&", query) : "");
        return SendAsync<List<ApiEvent>>(HttpMethod.Get, url);
    }

    public Task<ApiResponse<ApiEvent>> GetEventAsync(int id) =>
        SendAsync<ApiEvent>(HttpMethod.Get, $"api/events/{id}");

    public Task<ApiResponse<List<ApiEvent>>> GetMyEventsAsync() =>
        SendAsync<List<ApiEvent>>(HttpMethod.Get, "api/events/mine");

    public Task<ApiResponse<ApiEvent>> CreateEventAsync(EventFormViewModel form) =>
        SendAsync<ApiEvent>(HttpMethod.Post, "api/events", JsonBody(EventBody(form)));

    public Task<ApiResponse<ApiEvent>> UpdateEventAsync(int id, EventFormViewModel form) =>
        SendAsync<ApiEvent>(HttpMethod.Put, $"api/events/{id}", JsonBody(EventBody(form)));

    public async Task<ApiResponse> DeleteEventAsync(int id) =>
        await SendAsync<object>(HttpMethod.Delete, $"api/events/{id}");

    public Task<ApiResponse<ApiEvent>> UploadBannerAsync(int eventId, IFormFile file) =>
        SendAsync<ApiEvent>(HttpMethod.Post, $"api/events/{eventId}/banner", FileBody(file));

    public Task<ApiResponse<List<ApiEnrolment>>> GetEventEnrolmentsAsync(int eventId) =>
        SendAsync<List<ApiEnrolment>>(HttpMethod.Get, $"api/events/{eventId}/enrolments");

    // ------------------------------------------------------------------ Categories

    public Task<ApiResponse<ApiCategory>> CreateCategoryAsync(int eventId, CategoryFormViewModel form) =>
        SendAsync<ApiCategory>(HttpMethod.Post, $"api/events/{eventId}/categories", JsonBody(CategoryBody(form)));

    public Task<ApiResponse<ApiCategory>> UpdateCategoryAsync(int categoryId, CategoryFormViewModel form) =>
        SendAsync<ApiCategory>(HttpMethod.Put, $"api/categories/{categoryId}", JsonBody(CategoryBody(form)));

    public async Task<ApiResponse> DeleteCategoryAsync(int categoryId) =>
        await SendAsync<object>(HttpMethod.Delete, $"api/categories/{categoryId}");

    // ------------------------------------------------------------------ Enrolments

    public Task<ApiResponse<ApiEnrolment>> EnrolAsync(int categoryId) =>
        SendAsync<ApiEnrolment>(HttpMethod.Post, $"api/categories/{categoryId}/enrolments");

    public Task<ApiResponse<List<ApiEnrolment>>> GetMyEnrolmentsAsync() =>
        SendAsync<List<ApiEnrolment>>(HttpMethod.Get, "api/enrolments/me");

    public async Task<ApiResponse> CancelEnrolmentAsync(int enrolmentId) =>
        await SendAsync<object>(HttpMethod.Delete, $"api/enrolments/{enrolmentId}");

    // ------------------------------------------------------------------ Results

    public Task<ApiResponse<ApiRaceResult>> CaptureResultAsync(int enrolmentId, ResultFormViewModel form) =>
        SendAsync<ApiRaceResult>(HttpMethod.Post, $"api/enrolments/{enrolmentId}/results", JsonBody(ResultBody(form)));

    public Task<ApiResponse<ApiRaceResult>> UpdateResultAsync(int resultId, ResultFormViewModel form) =>
        SendAsync<ApiRaceResult>(HttpMethod.Put, $"api/results/{resultId}", JsonBody(ResultBody(form)));

    public Task<ApiResponse<List<ApiRaceResult>>> GetMyResultsAsync() =>
        SendAsync<List<ApiRaceResult>>(HttpMethod.Get, "api/results/me");

    // ------------------------------------------------------------------ Plumbing

    private static object EventBody(EventFormViewModel f) =>
        new { f.EventName, f.Description, f.EventDate, f.StartTime, f.Location, f.Province, f.EventType, f.DistanceKM };

    private static object CategoryBody(CategoryFormViewModel f) =>
        new { f.CategoryName, f.DistanceKM, f.Price, f.MaxParticipants };

    private static object ResultBody(ResultFormViewModel f) =>
        new { f.FinishTime, f.OverallPosition, f.CategoryPosition };

    private static HttpContent JsonBody(object body) => JsonContent.Create(body, options: Json);

    private static HttpContent FileBody(IFormFile file)
    {
        var part = new StreamContent(file.OpenReadStream());
        part.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);

        var content = new MultipartFormDataContent();
        content.Add(part, "file", file.FileName);
        return content;
    }

    /// <summary>Builds a request that carries the logged-in user's API session cookie.</summary>
    private HttpRequestMessage BuildRequest(HttpMethod method, string url, HttpContent? content = null)
    {
        var request = new HttpRequestMessage(method, url) { Content = content };

        var session = _accessor.HttpContext?.User.FindFirst(AuthConstants.ApiSessionClaim)?.Value;
        if (!string.IsNullOrEmpty(session))
            request.Headers.Add("Cookie", $"{AuthConstants.ApiCookieName}={session}");

        return request;
    }

    private async Task<ApiResponse<T>> SendAsync<T>(HttpMethod method, string url, HttpContent? content = null)
    {
        using var request = BuildRequest(method, url, content);
        try
        {
            using var response = await _http.SendAsync(request);

            // A logged-in user who is refused with 401 has lost their API session.
            if (response.StatusCode == HttpStatusCode.Unauthorized && request.Headers.Contains("Cookie"))
                throw new ApiSessionExpiredException();

            if (!response.IsSuccessStatusCode)
                return ApiResponse<T>.Fail(await ReadErrorAsync(response), response.StatusCode);

            if (response.StatusCode == HttpStatusCode.NoContent)
                return ApiResponse<T>.Ok(default, response.StatusCode);

            var data = await response.Content.ReadFromJsonAsync<T>(Json);
            return ApiResponse<T>.Ok(data, response.StatusCode);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return Unavailable<T>(ex);
        }
    }

    private ApiResponse<T> Unavailable<T>(Exception ex)
    {
        _logger.LogError(ex, "RaceDay API call failed");
        return ApiResponse<T>.Fail("RaceDay is temporarily unavailable. Please try again in a moment.",
            HttpStatusCode.ServiceUnavailable);
    }

    /// <summary>Turns an API error response into a short message that is safe to show to a user.</summary>
    private static async Task<string> ReadErrorAsync(HttpResponseMessage response)
    {
        if ((int)response.StatusCode >= 500)
            return "Something went wrong on our side. Please try again later.";

        try
        {
            var text = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(text);
            var root = doc.RootElement;

            // Our own errors look like { "message": "..." }
            if (root.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String)
                return message.GetString()!;

            // Automatic validation errors look like { "errors": { "Field": ["..."] } }
            if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
            {
                var messages = errors.EnumerateObject()
                    .SelectMany(p => p.Value.EnumerateArray().Select(v => v.GetString()))
                    .Where(s => !string.IsNullOrWhiteSpace(s));
                var joined = string.Join(" ", messages);
                if (joined.Length > 0) return joined;
            }
        }
        catch (JsonException)
        {
            // fall through to the generic messages below
        }

        return response.StatusCode switch
        {
            HttpStatusCode.Forbidden => "You do not have permission to do that.",
            HttpStatusCode.NotFound => "We could not find what you were looking for.",
            HttpStatusCode.Conflict => "That action conflicts with existing data.",
            _ => "The request could not be completed. Please check your details and try again."
        };
    }

    /// <summary>Finds the API's session cookie in a login response.</summary>
    private static string? ReadSessionCookie(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var values)) return null;

        var prefix = AuthConstants.ApiCookieName + "=";
        foreach (var header in values)
        {
            if (header.StartsWith(prefix, StringComparison.Ordinal))
                return header[prefix.Length..].Split(';')[0];
        }
        return null;
    }
}