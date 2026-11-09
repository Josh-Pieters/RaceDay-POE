using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Shows every Participant enrolled in one of the Organiser's events.</summary>
public class EventEnrolmentsController : OrganiserControllerBase
{
    public EventEnrolmentsController(IApiClient api) : base(api) { }

    public async Task<IActionResult> Index(int? eventId)
    {
        if (eventId == null) return await EventPicker("View enrolments", "EventEnrolments");

        var ev = await Api.GetEventAsync(eventId.Value);
        var enrolments = await Api.GetEventEnrolmentsAsync(eventId.Value);

        if (!ev.Success || ev.Data == null || !enrolments.Success)
        {
            SetError(!ev.Success ? ev.Error : enrolments.Error);
            return RedirectToAction("Events", "Organiser");
        }

        return View(new EventEnrolmentsViewModel { Event = ev.Data, Enrolments = enrolments.Data ?? new() });
    }
}