using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Capture and publish finish times and positions for the Participants of an event.</summary>
public class ResultEntryController : OrganiserControllerBase
{
    public ResultEntryController(IApiClient api) : base(api) { }

    public async Task<IActionResult> Index(int? eventId)
    {
        if (eventId == null) return await EventPicker("Enter results", "ResultEntry");

        var ev = await Api.GetEventAsync(eventId.Value);
        var enrolments = await Api.GetEventEnrolmentsAsync(eventId.Value);

        if (!ev.Success || ev.Data == null || !enrolments.Success)
        {
            SetError(!ev.Success ? ev.Error : enrolments.Error);
            return RedirectToAction("Events", "Organiser");
        }

        // Only confirmed enrolments can have a result.
        var confirmed = (enrolments.Data ?? new()).Where(e => e.Status == Lookups.Confirmed).ToList();
        return View(new EventEnrolmentsViewModel { Event = ev.Data, Enrolments = confirmed });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(ResultFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            SetError("Please enter a finish time and both positions (1 or higher).");
            return RedirectToAction(nameof(Index), new { eventId = model.EventID });
        }

        var result = model.ResultID.HasValue
            ? await Api.UpdateResultAsync(model.ResultID.Value, model)
            : await Api.CaptureResultAsync(model.EnrolmentID, model);

        if (result.Success)
            SetSuccess("Result saved and published.");
        else
            SetError(result.Error);

        return RedirectToAction(nameof(Index), new { eventId = model.EventID });
    }
}