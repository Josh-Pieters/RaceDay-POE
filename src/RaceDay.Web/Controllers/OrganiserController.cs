using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>The Organiser's dashboard and event management (create, edit, delete, banner upload).</summary>
public class OrganiserController : OrganiserControllerBase
{
    public OrganiserController(IApiClient api) : base(api) { }

    public async Task<IActionResult> Dashboard()
    {
        var result = await Api.GetMyEventsAsync();
        if (!result.Success) ViewData["Error"] = result.Error;

        var events = result.Data ?? new List<ApiEvent>();
        var today = DateOnly.FromDateTime(DateTime.Today);

        return View(new OrganiserDashboardViewModel
        {
            Events = events,
            TotalEvents = events.Count,
            UpcomingCount = events.Count(e => e.EventDate >= today),
            TotalEnrolments = events.Sum(e => e.EnrolmentCount ?? 0),
            NextEvent = events.Where(e => e.EventDate >= today).OrderBy(e => e.EventDate).FirstOrDefault()
        });
    }

    public async Task<IActionResult> Events()
    {
        var result = await Api.GetMyEventsAsync();
        if (!result.Success) ViewData["Error"] = result.Error;
        return View(result.Data ?? new List<ApiEvent>());
    }

    [HttpGet]
    public IActionResult CreateEvent() => View(new EventFormViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateEvent(EventFormViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var created = await Api.CreateEventAsync(model);
        if (!created.Success || created.Data == null)
        {
            ModelState.AddModelError(string.Empty, created.Error);
            return View(model);
        }

        // The banner is a second call: the API stores the file in Azure Blob Storage.
        if (model.Banner is { Length: > 0 })
        {
            var banner = await Api.UploadBannerAsync(created.Data.EventID, model.Banner);
            if (!banner.Success)
                SetWarning($"The event was created, but the banner could not be uploaded: {banner.Error}");
            else
                SetSuccess("Event created. Now add the categories people can enter.");
        }
        else
        {
            SetSuccess("Event created. Now add the categories people can enter.");
        }

        return RedirectToAction("Index", "Categories", new { eventId = created.Data.EventID });
    }

    [HttpGet]
    public async Task<IActionResult> EditEvent(int id)
    {
        var result = await Api.GetEventAsync(id);
        if (!result.Success || result.Data == null)
        {
            SetError(result.Error);
            return RedirectToAction(nameof(Events));
        }

        var ev = result.Data;
        return View(new EventFormViewModel
        {
            EventID = ev.EventID,
            EventName = ev.EventName,
            Description = ev.Description,
            EventDate = ev.EventDate,
            StartTime = ev.StartTime,
            Location = ev.Location,
            Province = ev.Province,
            EventType = ev.EventType,
            DistanceKM = ev.DistanceKM,
            CurrentBannerUrl = ev.BannerImageUrl
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditEvent(int id, EventFormViewModel model)
    {
        model.EventID = id;
        if (!ModelState.IsValid) return View(model);

        var updated = await Api.UpdateEventAsync(id, model);
        if (!updated.Success)
        {
            ModelState.AddModelError(string.Empty, updated.Error);
            return View(model);
        }

        if (model.Banner is { Length: > 0 })
        {
            var banner = await Api.UploadBannerAsync(id, model.Banner);
            if (!banner.Success)
            {
                SetWarning($"The event was saved, but the banner could not be uploaded: {banner.Error}");
                return RedirectToAction(nameof(Events));
            }
        }

        SetSuccess("Event updated.");
        return RedirectToAction(nameof(Events));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var result = await Api.DeleteEventAsync(id);
        if (result.Success)
            SetSuccess("Event deleted.");
        else
            SetError(result.Error);

        return RedirectToAction(nameof(Events));
    }
}