using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;

namespace RaceDay.Tests;

public class AuthTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public AuthTests(RaceDayFactory factory) => _factory = factory;

    private static object NewUser(string role = "Participant", string? email = null, string? code = null) => new
    {
        fullName = "Test User",
        email = email ?? $"user-{Guid.NewGuid():N}@test.com",
        password = TestHelpers.Password,
        role,
        organiserCode = code
    };

    [Fact]
    public async Task Register_Participant_Returns201AndUserWithoutPassword()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", NewUser("Participant"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"role\":\"Participant\"", body);
        Assert.DoesNotContain("passwordHash", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Register_Organiser_WithValidCode_Returns201()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register",
            NewUser("Organiser", code: TestHelpers.OrganiserCode));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Register_Organiser_WithoutCode_Returns403()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", NewUser("Organiser"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var client = _factory.CreateClient();
        var email = $"dupe-{Guid.NewGuid():N}@test.com";

        var first = await client.PostAsJsonAsync("/api/auth/register", NewUser(email: email));
        var second = await client.PostAsJsonAsync("/api/auth/register", NewUser(email: email));

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Register_InvalidRole_Returns400()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", NewUser("Admin"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_InvalidEmailAndShortPassword_Returns400()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Bad Input",
            email = "not-an-email",
            password = "short",
            role = "Participant"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsOkWithRole()
    {
        var client = _factory.CreateClient();
        var email = $"login-{Guid.NewGuid():N}@test.com";
        await client.PostAsJsonAsync("/api/auth/register", NewUser(email: email));

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = TestHelpers.Password });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var user = await response.Content.ReadFromJsonAsync<UserResponse>();
        Assert.Equal("Participant", user!.Role);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var client = _factory.CreateClient();
        var email = $"wrongpw-{Guid.NewGuid():N}@test.com";
        await client.PostAsJsonAsync("/api/auth/register", NewUser(email: email));

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "WrongPassword1" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_UnknownEmail_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new { email = "nobody@test.com", password = TestHelpers.Password });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Register_StoresHashedPassword_NotPlainText()
    {
        var client = _factory.CreateClient();
        var email = $"hash-{Guid.NewGuid():N}@test.com";
        await client.PostAsJsonAsync("/api/auth/register", NewUser(email: email));

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
        var user = await db.Users.SingleAsync(u => u.Email == email);

        Assert.False(string.IsNullOrWhiteSpace(user.PasswordHash));
        Assert.NotEqual(TestHelpers.Password, user.PasswordHash);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutLogin_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_AfterLogin_ReturnsOwnProfile()
    {
        var client = await TestHelpers.LoggedInClientAsync(_factory, "Participant");

        var response = await client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var user = await response.Content.ReadFromJsonAsync<UserResponse>();
        Assert.Equal("Participant", user!.Role);
    }

    [Fact]
    public async Task Logout_EndsSession()
    {
        var client = await TestHelpers.LoggedInClientAsync(_factory, "Participant");

        var logout = await client.PostAsync("/api/auth/logout", null);
        var afterLogout = await client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, afterLogout.StatusCode);
    }

    [Fact]
    public async Task UpdateProfile_BothRoles_CanSaveProfile()
    {
        var participant = await TestHelpers.LoggedInClientAsync(_factory, "Participant");
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var profile = new { dateOfBirth = "1990-05-20", gender = "Female", emergencyContactName = "Mum", emergencyContactPhone = "0821234567" };

        var participantResponse = await participant.PutAsJsonAsync("/api/users/me/profile", profile);
        var organiserResponse = await organiser.PutAsJsonAsync("/api/users/me/profile", profile);

        Assert.Equal(HttpStatusCode.OK, participantResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, organiserResponse.StatusCode);
    }
}
