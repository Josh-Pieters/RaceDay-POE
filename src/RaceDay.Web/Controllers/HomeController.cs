using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class HomeController : AppController
{
    private readonly IApiClient _api;

    public HomeController(IApiClient api) => _api = api;

    /// <summary>Upcoming events with filters. Public - no login needed.</summary>
    public async Task<IActionResult> Index([FromQuery] EventFilter filter)
    {
        var result = await _api.GetEventsAsync(filter);
        if (!result.Success) ViewData["Error"] = result.Error;

        return View(new HomeViewModel { Filter = filter, Events = result.Data ?? new() });
    }

    /// <summary>Friendly error page. Raw error details are never shown to users.</summary>
    public IActionResult Error(int? code)
    {
        var status = code ?? Response.StatusCode;
        var model = new ErrorViewModel { Code = status };

        if (status == 404)
        {
            model.Title = "Page not found";
            model.Message = "We could not find the page you were looking for.";
        }
        else if (status == 403)
        {
            model.Title = "Access denied";
            model.Message = "You do not have permission to view this page.";
        }

        return View(model);
    }
}