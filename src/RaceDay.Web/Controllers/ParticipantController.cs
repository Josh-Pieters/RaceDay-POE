using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[Authorize(Roles = Lookups.Participant)]
public class ParticipantController : AppController
{
    private readonly IApiClient _api;

    public ParticipantController(IApiClient api) => _api = api;

    public async Task<IActionResult> MyEnrolments()
    {
        var result = await _api.GetMyEnrolmentsAsync();
        if (!result.Success) ViewData["Error"] = result.Error;
        return View(result.Data ?? new());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelEnrolment(int id)
    {
        var result = await _api.CancelEnrolmentAsync(id);
        if (result.Success)
            SetSuccess("Your enrolment has been cancelled.");
        else
            SetError(result.Error);

        return RedirectToAction(nameof(MyEnrolments));
    }

    public async Task<IActionResult> MyResults()
    {
        var result = await _api.GetMyResultsAsync();
        if (!result.Success) ViewData["Error"] = result.Error;
        return View(result.Data ?? new());
    }
}