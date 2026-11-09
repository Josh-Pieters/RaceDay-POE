using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[Authorize]
public class ProfileController : AppController
{
    private readonly IApiClient _api;

    public ProfileController(IApiClient api) => _api = api;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var result = await _api.GetMeAsync();
        if (!result.Success || result.Data == null)
        {
            SetError(result.Error);
            return RedirectToAction("Index", "Home");
        }
        return View(ToViewModel(result.Data));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(ProfileViewModel model)
    {
        if (!ModelState.IsValid) return View("Index", model);

        var user = await _api.UpdateMeAsync(model.FullName, model.PhoneNumber);
        if (!user.Success)
        {
            ModelState.AddModelError(string.Empty, user.Error);
            return View("Index", model);
        }

        var profile = await _api.UpdateProfileAsync(model);
        if (!profile.Success)
        {
            ModelState.AddModelError(string.Empty, profile.Error);
            return View("Index", model);
        }

        await RefreshDisplayNameAsync(model.FullName);
        SetSuccess("Your profile has been updated.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadPicture(IFormFile? picture)
    {
        if (picture == null || picture.Length == 0)
        {
            SetError("Please choose an image to upload.");
            return RedirectToAction(nameof(Index));
        }

        var result = await _api.UploadProfilePictureAsync(picture);
        if (result.Success)
            SetSuccess("Your profile picture has been updated.");
        else
            SetError(result.Error);

        return RedirectToAction(nameof(Index));
    }

    private static ProfileViewModel ToViewModel(ApiUser user) => new()
    {
        Email = user.Email,
        Role = user.Role,
        FullName = user.FullName,
        PhoneNumber = user.PhoneNumber,
        DateOfBirth = user.Profile?.DateOfBirth,
        Gender = user.Profile?.Gender,
        EmergencyContactName = user.Profile?.EmergencyContactName,
        EmergencyContactPhone = user.Profile?.EmergencyContactPhone,
        MedicalNotes = user.Profile?.MedicalNotes,
        ProfilePictureUrl = user.Profile?.ProfilePictureUrl
    };

    /// <summary>Updates the name shown in the menu without changing the session's expiry time.</summary>
    private async Task RefreshDisplayNameAsync(string fullName)
    {
        var authResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        if (authResult.Principal?.Identity is not ClaimsIdentity identity) return;

        var oldName = identity.FindFirst(ClaimTypes.Name);
        if (oldName != null) identity.RemoveClaim(oldName);
        identity.AddClaim(new Claim(ClaimTypes.Name, fullName));

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity), authResult.Properties);
    }
}