using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Add, edit and delete the age or distance categories of an event.</summary>
public class CategoriesController : OrganiserControllerBase
{
    public CategoriesController(IApiClient api) : base(api) { }

    public async Task<IActionResult> Index(int? eventId)
    {
        if (eventId == null) return await EventPicker("Manage categories", "Categories");
        return await RenderIndexAsync(eventId.Value, new CategoryFormViewModel { EventID = eventId.Value });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(CategoryFormViewModel model)
    {
        if (!ModelState.IsValid) return await RenderIndexAsync(model.EventID, model);

        var result = await Api.CreateCategoryAsync(model.EventID, model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            return await RenderIndexAsync(model.EventID, model);
        }

        SetSuccess("Category added.");
        return RedirectToAction(nameof(Index), new { eventId = model.EventID });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, int eventId)
    {
        var ev = await Api.GetEventAsync(eventId);
        var category = ev.Data?.Categories?.FirstOrDefault(c => c.CategoryID == id);
        if (category == null)
        {
            SetError("That category could not be found.");
            return RedirectToAction(nameof(Index), new { eventId });
        }

        ViewData["EventName"] = ev.Data!.EventName;
        return View(new CategoryFormViewModel
        {
            CategoryID = category.CategoryID,
            EventID = eventId,
            CategoryName = category.CategoryName,
            DistanceKM = category.DistanceKM,
            Price = category.Price,
            MaxParticipants = category.MaxParticipants
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CategoryFormViewModel model)
    {
        model.CategoryID = id;
        if (!ModelState.IsValid) return View(model);

        var result = await Api.UpdateCategoryAsync(id, model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            return View(model);
        }

        SetSuccess("Category updated.");
        return RedirectToAction(nameof(Index), new { eventId = model.EventID });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, int eventId)
    {
        var result = await Api.DeleteCategoryAsync(id);
        if (result.Success)
            SetSuccess("Category deleted.");
        else
            SetError(result.Error);

        return RedirectToAction(nameof(Index), new { eventId });
    }

    private async Task<IActionResult> RenderIndexAsync(int eventId, CategoryFormViewModel form)
    {
        var ev = await Api.GetEventAsync(eventId);
        if (!ev.Success || ev.Data == null)
        {
            SetError(ev.Error);
            return RedirectToAction("Events", "Organiser");
        }

        return View("Index", new CategoriesViewModel { Event = ev.Data, NewCategory = form });
    }
}