using System.Net;
using System.Net.Http.Json;

namespace RaceDay.Tests;

/// <summary>Verifies that role-based access is enforced for authenticated and unauthenticated requests.</summary>
public class RoleAccessTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public RoleAccessTests(RaceDayFactory factory) => _factory = factory;

    [Fact]
    public async Task Anonymous_CreateEvent_Returns401()
    {
        var anonymous = _factory.CreateClient();

        var response = await anonymous.PostAsJsonAsync("/api/events", TestHelpers.EventPayload());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_Enrol_Returns401()
    {
        var anonymous = _factory.CreateClient();

        var response = await anonymous.PostAsync("/api/categories/1/enrolments", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CreateEvent_Returns403()
    {
        var participant = await TestHelpers.LoggedInClientAsync(_factory, "Participant");

        var response = await participant.PostAsJsonAsync("/api/events", TestHelpers.EventPayload());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_UpdateOrDeleteEvent_Returns403()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var participant = await TestHelpers.LoggedInClientAsync(_factory, "Participant");
        var ev = await TestHelpers.CreateEventAsync(organiser);

        var update = await participant.PutAsJsonAsync($"/api/events/{ev.EventID}", TestHelpers.EventPayload("Nope"));
        var delete = await participant.DeleteAsync($"/api/events/{ev.EventID}");

        Assert.Equal(HttpStatusCode.Forbidden, update.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
    }

    [Fact]
    public async Task Participant_CreateCategory_Returns403()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var participant = await TestHelpers.LoggedInClientAsync(_factory, "Participant");
        var ev = await TestHelpers.CreateEventAsync(organiser);

        var response = await participant.PostAsJsonAsync($"/api/events/{ev.EventID}/categories",
            new { categoryName = "Sneaky", distanceKM = 5, price = 0, maxParticipants = 10 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_ViewEventEnrolments_Returns403()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var participant = await TestHelpers.LoggedInClientAsync(_factory, "Participant");
        var ev = await TestHelpers.CreateEventAsync(organiser);

        var response = await participant.GetAsync($"/api/events/{ev.EventID}/enrolments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CaptureResult_Returns403()
    {
        var participant = await TestHelpers.LoggedInClientAsync(_factory, "Participant");

        var response = await participant.PostAsJsonAsync("/api/enrolments/1/results",
            new { finishTime = "01:00:00", overallPosition = 1, categoryPosition = 1 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_EnrolInCategory_Returns403()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var ev = await TestHelpers.CreateEventAsync(organiser);
        var category = await TestHelpers.CreateCategoryAsync(organiser, ev.EventID);

        var response = await organiser.PostAsync($"/api/categories/{category.CategoryID}/enrolments", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_ViewParticipantOnlyEndpoints_Returns403()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");

        var enrolments = await organiser.GetAsync("/api/enrolments/me");
        var results = await organiser.GetAsync("/api/results/me");

        Assert.Equal(HttpStatusCode.Forbidden, enrolments.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, results.StatusCode);
    }

    [Fact]
    public async Task Participant_ViewOrganiserOnlyUserLookup_Returns403()
    {
        var participant = await TestHelpers.LoggedInClientAsync(_factory, "Participant");

        var response = await participant.GetAsync("/api/users/1");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task OrganiserOfAnotherEvent_ViewEnrolments_Returns403()
    {
        var owner = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var other = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var ev = await TestHelpers.CreateEventAsync(owner);

        var response = await other.GetAsync($"/api/events/{ev.EventID}/enrolments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
