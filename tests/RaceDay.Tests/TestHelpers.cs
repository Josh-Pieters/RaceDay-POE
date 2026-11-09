using System.Net.Http.Headers;
using System.Net.Http.Json;
using RaceDay.Api.DTOs;

namespace RaceDay.Tests;

public static class TestHelpers
{
    public const string Password = "Passw0rd!";
    public const string OrganiserCode = "TEST-ORGANISER-CODE";

    /// <summary>Registers a fresh user with the given role and logs them in. The client keeps the session cookie.</summary>
    public static async Task<HttpClient> LoggedInClientAsync(RaceDayFactory factory, string role)
    {
        var client = factory.CreateClient();
        var email = $"{role.ToLowerInvariant()}-{Guid.NewGuid():N}@test.com";

        var register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = $"Test {role}",
            email,
            password = Password,
            role,
            organiserCode = role == "Organiser" ? OrganiserCode : null
        });
        register.EnsureSuccessStatusCode();

        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = Password });
        login.EnsureSuccessStatusCode();

        return client;
    }

    public static object EventPayload(string name = "Test 10km Run") => new
    {
        eventName = name,
        description = "A test event",
        eventDate = DateOnly.FromDateTime(DateTime.Today).AddDays(30),
        startTime = "06:00:00",
        location = "Green Point, Cape Town",
        province = "Western Cape",
        eventType = "Run",
        distanceKM = 10
    };

    public static async Task<EventResponse> CreateEventAsync(HttpClient organiser, string name = "Test 10km Run")
    {
        var response = await organiser.PostAsJsonAsync("/api/events", EventPayload(name));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EventResponse>())!;
    }

    public static async Task<CategoryResponse> CreateCategoryAsync(HttpClient organiser, int eventId, int maxParticipants = 100)
    {
        var response = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories", new
        {
            categoryName = "Senior 10km",
            distanceKM = 10,
            price = 150,
            maxParticipants
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CategoryResponse>())!;
    }

    public static async Task<EnrolmentResponse> EnrolAsync(HttpClient participant, int categoryId)
    {
        var response = await participant.PostAsync($"/api/categories/{categoryId}/enrolments", null);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EnrolmentResponse>())!;
    }

    /// <summary>Builds a multipart upload body containing one file named "file".</summary>
    public static MultipartFormDataContent ImageForm(string fileName = "banner.png",
        string contentType = "image/png", byte[]? bytes = null)
    {
        // These first bytes are the PNG signature, so the API's image check accepts them.
        bytes ??= new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x00 };

        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        var form = new MultipartFormDataContent();
        form.Add(file, "file", fileName);
        return form;
    }
}