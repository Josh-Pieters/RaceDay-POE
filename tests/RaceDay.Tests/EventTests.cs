using System.Net;
using System.Net.Http.Json;
using RaceDay.Api.DTOs;

namespace RaceDay.Tests;

public class EventTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public EventTests(RaceDayFactory factory) => _factory = factory;

    [Fact]
    public async Task Organiser_CanCreateEvent_Returns201WithEventTypeAndDistance()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");

        var response = await organiser.PostAsJsonAsync("/api/events", TestHelpers.EventPayload("Create Test"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var ev = await response.Content.ReadFromJsonAsync<EventResponse>();
        Assert.Equal("Create Test", ev!.EventName);
        Assert.Equal("Run", ev.EventType);
        Assert.Equal(10m, ev.DistanceKM);
    }

    [Fact]
    public async Task CreateEvent_InvalidEventType_Returns400()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");

        var response = await organiser.PostAsJsonAsync("/api/events", new
        {
            eventName = "Bad Type",
            eventDate = DateOnly.FromDateTime(DateTime.Today).AddDays(10),
            location = "Cape Town",
            eventType = "Swim",
            distanceKM = 5
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_MissingName_Returns400()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");

        var response = await organiser.PostAsJsonAsync("/api/events", new
        {
            eventDate = DateOnly.FromDateTime(DateTime.Today).AddDays(10),
            location = "Cape Town",
            eventType = "Run",
            distanceKM = 5
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanUpdateOwnEvent()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var ev = await TestHelpers.CreateEventAsync(organiser, "Before Update");

        var response = await organiser.PutAsJsonAsync($"/api/events/{ev.EventID}", TestHelpers.EventPayload("After Update"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<EventResponse>();
        Assert.Equal("After Update", updated!.EventName);
    }

    [Fact]
    public async Task Organiser_CanDeleteOwnEvent_ThenItIsGone()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var ev = await TestHelpers.CreateEventAsync(organiser, "To Delete");

        var delete = await organiser.DeleteAsync($"/api/events/{ev.EventID}");
        var get = await organiser.GetAsync($"/api/events/{ev.EventID}");

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotEditAnotherOrganisersEvent_Returns403()
    {
        var owner = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var other = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var ev = await TestHelpers.CreateEventAsync(owner);

        var update = await other.PutAsJsonAsync($"/api/events/{ev.EventID}", TestHelpers.EventPayload("Hijacked"));
        var delete = await other.DeleteAsync($"/api/events/{ev.EventID}");

        Assert.Equal(HttpStatusCode.Forbidden, update.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
    }

    [Fact]
    public async Task Update_NonExistentEvent_Returns404()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");

        var response = await organiser.PutAsJsonAsync("/api/events/999999", TestHelpers.EventPayload());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_CanViewEventsAndEventDetails()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var name = $"Public Event {Guid.NewGuid():N}";
        var ev = await TestHelpers.CreateEventAsync(organiser, name);
        await TestHelpers.CreateCategoryAsync(organiser, ev.EventID);
        var anonymous = _factory.CreateClient();

        var list = await anonymous.GetAsync("/api/events");
        var detail = await anonymous.GetAsync($"/api/events/{ev.EventID}");

        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.Contains(name, await list.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
        var body = await detail.Content.ReadFromJsonAsync<EventResponse>();
        Assert.Single(body!.Categories!);
    }

    [Fact]
    public async Task Organiser_CanManageCategories()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var ev = await TestHelpers.CreateEventAsync(organiser);
        var category = await TestHelpers.CreateCategoryAsync(organiser, ev.EventID);

        var update = await organiser.PutAsJsonAsync($"/api/categories/{category.CategoryID}",
            new { categoryName = "Under 20", distanceKM = 10, price = 100, maxParticipants = 50 });
        var delete = await organiser.DeleteAsync($"/api/categories/{category.CategoryID}");

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
    }
}
