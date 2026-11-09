using System.Net;
using System.Net.Http.Json;
using RaceDay.Api.DTOs;

namespace RaceDay.Tests;

public class DashboardAndResultTotalsTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public DashboardAndResultTotalsTests(RaceDayFactory factory) => _factory = factory;

    [Fact]
    public async Task Organiser_GetMyEvents_ReturnsOnlyOwnEventsWithEnrolmentCounts()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var otherOrganiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var p1 = await TestHelpers.LoggedInClientAsync(_factory, "Participant");
        var p2 = await TestHelpers.LoggedInClientAsync(_factory, "Participant");

        var ownName = $"Own {Guid.NewGuid():N}";
        var otherName = $"Other {Guid.NewGuid():N}";
        var ev = await TestHelpers.CreateEventAsync(organiser, ownName);
        await TestHelpers.CreateEventAsync(otherOrganiser, otherName);
        var category = await TestHelpers.CreateCategoryAsync(organiser, ev.EventID);
        await TestHelpers.EnrolAsync(p1, category.CategoryID);
        await TestHelpers.EnrolAsync(p2, category.CategoryID);

        var mine = await organiser.GetFromJsonAsync<List<EventResponse>>("/api/events/mine");

        var own = Assert.Single(mine!, e => e.EventName == ownName);
        Assert.Equal(2, own.EnrolmentCount);
        Assert.DoesNotContain(mine!, e => e.EventName == otherName);
    }

    [Fact]
    public async Task Participant_GetMyEvents_Returns403()
    {
        var participant = await TestHelpers.LoggedInClientAsync(_factory, "Participant");

        var response = await participant.GetAsync("/api/events/mine");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task MyResults_IncludeTotalFinishersForTheEvent()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var p1 = await TestHelpers.LoggedInClientAsync(_factory, "Participant");
        var p2 = await TestHelpers.LoggedInClientAsync(_factory, "Participant");
        var ev = await TestHelpers.CreateEventAsync(organiser);
        var category = await TestHelpers.CreateCategoryAsync(organiser, ev.EventID);
        var e1 = await TestHelpers.EnrolAsync(p1, category.CategoryID);
        var e2 = await TestHelpers.EnrolAsync(p2, category.CategoryID);

        await organiser.PostAsJsonAsync($"/api/enrolments/{e1.EnrolmentID}/results",
            new { finishTime = "00:50:00", overallPosition = 1, categoryPosition = 1 });
        await organiser.PostAsJsonAsync($"/api/enrolments/{e2.EnrolmentID}/results",
            new { finishTime = "00:55:00", overallPosition = 2, categoryPosition = 2 });

        var results = await p1.GetFromJsonAsync<List<ResultResponse>>("/api/results/me");

        var result = Assert.Single(results!);
        Assert.Equal(1, result.OverallPosition);
        Assert.Equal(2, result.TotalFinishers);
    }
}
