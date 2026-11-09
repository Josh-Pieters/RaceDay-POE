using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Base class for the Organiser-only pages. Participants are refused automatically.</summary>
[Authorize(Roles = Lookups.Organiser)]
public abstract class OrganiserControllerBase : AppController
{
    protected readonly IApiClient Api;

    protected OrganiserControllerBase(IApiClient api) => Api = api;

    /// <summary>Shows a list of the Organiser's events to choose from (used when a menu link has no event yet).</summary>
    protected async Task<IActionResult> EventPicker(string title, string controllerName)
    {
        var events = await Api.GetMyEventsAsync();
        return View("~/Views/Shared/EventPicker.cshtml", new EventPickerViewModel
        {
            Title = title,
            ControllerName = controllerName,
            Events = events.Data ?? new(),
            Error = events.Success ? null : events.Error
        });
    }
}