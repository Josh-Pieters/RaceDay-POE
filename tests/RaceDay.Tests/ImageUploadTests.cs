using System.Net;
using System.Net.Http.Json;
using System.Text;
using RaceDay.Api.DTOs;

namespace RaceDay.Tests;

public class ImageUploadTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public ImageUploadTests(RaceDayFactory factory) => _factory = factory;

    [Fact]
    public async Task Organiser_CanUploadBanner_AndEventShowsBannerUrl()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var ev = await TestHelpers.CreateEventAsync(organiser);

        var response = await organiser.PostAsync($"/api/events/{ev.EventID}/banner", TestHelpers.ImageForm());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<EventResponse>();
        Assert.Contains("/banners/", updated!.BannerImageUrl);

        var fetched = await organiser.GetFromJsonAsync<EventResponse>($"/api/events/{ev.EventID}");
        Assert.Equal(updated.BannerImageUrl, fetched!.BannerImageUrl);
    }

    [Fact]
    public async Task Participant_UploadBanner_Returns403()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var participant = await TestHelpers.LoggedInClientAsync(_factory, "Participant");
        var ev = await TestHelpers.CreateEventAsync(organiser);

        var response = await participant.PostAsync($"/api/events/{ev.EventID}/banner", TestHelpers.ImageForm());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_UploadBanner_Returns401()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var ev = await TestHelpers.CreateEventAsync(organiser);
        var anonymous = _factory.CreateClient();

        var response = await anonymous.PostAsync($"/api/events/{ev.EventID}/banner", TestHelpers.ImageForm());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotUploadBannerForAnotherOrganisersEvent_Returns403()
    {
        var owner = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var other = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var ev = await TestHelpers.CreateEventAsync(owner);

        var response = await other.PostAsync($"/api/events/{ev.EventID}/banner", TestHelpers.ImageForm());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UploadBanner_FileThatIsNotReallyAnImage_Returns400()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var ev = await TestHelpers.CreateEventAsync(organiser);
        var fakeImage = TestHelpers.ImageForm("evil.png", "image/png", Encoding.UTF8.GetBytes("this is just text"));

        var response = await organiser.PostAsync($"/api/events/{ev.EventID}/banner", fakeImage);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UploadBanner_WrongContentType_Returns400()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var ev = await TestHelpers.CreateEventAsync(organiser);
        var pdf = TestHelpers.ImageForm("document.pdf", "application/pdf");

        var response = await organiser.PostAsync($"/api/events/{ev.EventID}/banner", pdf);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CanUploadProfilePicture_AndItAppearsOnProfile()
    {
        var participant = await TestHelpers.LoggedInClientAsync(_factory, "Participant");

        var upload = await participant.PostAsync("/api/users/me/profile-picture", TestHelpers.ImageForm("me.png"));
        var me = await participant.GetFromJsonAsync<UserResponse>("/api/users/me");

        Assert.Equal(HttpStatusCode.OK, upload.StatusCode);
        Assert.Contains("/profiles/", me!.Profile!.ProfilePictureUrl);
    }
}