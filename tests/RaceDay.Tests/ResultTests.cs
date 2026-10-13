using System.Net;
using System.Net.Http.Json;
using RaceDay.Api.DTOs;

namespace RaceDay.Tests;

public class ResultTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public ResultTests(RaceDayFactory factory) => _factory = factory;

    private static object ResultPayload(string time = "00:52:10", int overall = 12, int category = 3) =>
        new { finishTime = time, overallPosition = overall, categoryPosition = category };

    [Fact]
    public async Task Organiser_CanCaptureResult_AndParticipantCanViewIt()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var participant = await TestHelpers.LoggedInClientAsync(_factory, "Participant");
        var ev = await TestHelpers.CreateEventAsync(organiser);
        var category = await TestHelpers.CreateCategoryAsync(organiser, ev.EventID);
        var enrolment = await TestHelpers.EnrolAsync(participant, category.CategoryID);

        var capture = await organiser.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentID}/results", ResultPayload());
        var mine = await participant.GetFromJsonAsync<List<ResultResponse>>("/api/results/me");

        Assert.Equal(HttpStatusCode.Created, capture.StatusCode);
        var result = Assert.Single(mine!);
        Assert.Equal(12, result.OverallPosition);
        Assert.Equal(3, result.CategoryPosition);
        Assert.Equal(new TimeOnly(0, 52, 10), result.FinishTime);
    }

    [Fact]
    public async Task CaptureResult_Twice_Returns409()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var participant = await TestHelpers.LoggedInClientAsync(_factory, "Participant");
        var ev = await TestHelpers.CreateEventAsync(organiser);
        var category = await TestHelpers.CreateCategoryAsync(organiser, ev.EventID);
        var enrolment = await TestHelpers.EnrolAsync(participant, category.CategoryID);
        await organiser.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentID}/results", ResultPayload());

        var again = await organiser.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentID}/results", ResultPayload());

        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotCaptureResultForAnotherOrganisersEvent_Returns403()
    {
        var owner = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var other = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var participant = await TestHelpers.LoggedInClientAsync(_factory, "Participant");
        var ev = await TestHelpers.CreateEventAsync(owner);
        var category = await TestHelpers.CreateCategoryAsync(owner, ev.EventID);
        var enrolment = await TestHelpers.EnrolAsync(participant, category.CategoryID);

        var response = await other.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentID}/results", ResultPayload());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_InvalidBody_Returns400()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var participant = await TestHelpers.LoggedInClientAsync(_factory, "Participant");
        var ev = await TestHelpers.CreateEventAsync(organiser);
        var category = await TestHelpers.CreateCategoryAsync(organiser, ev.EventID);
        var enrolment = await TestHelpers.EnrolAsync(participant, category.CategoryID);

        var response = await organiser.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentID}/results",
            new { overallPosition = 0 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_CanViewEventResultsLeaderboard()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var participant = await TestHelpers.LoggedInClientAsync(_factory, "Participant");
        var ev = await TestHelpers.CreateEventAsync(organiser);
        var category = await TestHelpers.CreateCategoryAsync(organiser, ev.EventID);
        var enrolment = await TestHelpers.EnrolAsync(participant, category.CategoryID);
        await organiser.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentID}/results", ResultPayload());
        var anonymous = _factory.CreateClient();

        var response = await anonymous.GetAsync($"/api/events/{ev.EventID}/results");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var results = await response.Content.ReadFromJsonAsync<List<ResultResponse>>();
        Assert.Single(results!);
    }
}
