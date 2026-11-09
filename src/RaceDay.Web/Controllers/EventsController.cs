using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class EventsController : AppController
{
    private readonly IApiClient _api;

    public EventsController(IApiClient api) => _api = api;

    /// <summary>Full event information and categories. Public; Participants also see the enrol form.</summary>
    public async Task<IActionResult> Details(int id)
    {
        var result = await _api.GetEventAsync(id);
        if (!result.Success || result.Data == null)
        {
            SetError(result.StatusCode == System.Net.HttpStatusCode.NotFound
                ? "That event could not be found."
                : result.Error);
            return RedirectToAction("Index", "Home");
        }

        var model = new EventDetailsViewModel { Event = result.Data };

        if (User.IsInRole(Lookups.Participant))
        {
            var mine = await _api.GetMyEnrolmentsAsync();
            model.MyEnrolment = mine.Data?.FirstOrDefault(e => e.EventID == id && e.Status == Lookups.Confirmed);
        }

        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = Lookups.Participant)]
    public async Task<IActionResult> Enrol(int eventId, int? categoryId)
    {
        if (categoryId == null)
        {
            SetError("Please choose a category before enrolling.");
            return RedirectToAction(nameof(Details), new { id = eventId });
        }

        var result = await _api.EnrolAsync(categoryId.Value);
        if (result.Success)
            SetSuccess("You are enrolled! Good luck on race day.");
        else
            SetError(result.Error);

        return RedirectToAction(nameof(Details), new { id = eventId });
    }
}