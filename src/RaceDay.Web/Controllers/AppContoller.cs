using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace RaceDay.Web.Controllers;

/// <summary>Shared helpers for every controller: flash messages shown at the top of the next page.</summary>
public abstract class AppController : Controller
{
    protected int CurrentUserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    protected void SetSuccess(string message) => TempData["Success"] = message;
    protected void SetError(string message) => TempData["Error"] = message;
    protected void SetWarning(string message) => TempData["Warning"] = message;
}