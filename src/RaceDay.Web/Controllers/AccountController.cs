using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class AccountController : AppController
{
    private readonly IApiClient _api;

    public AccountController(IApiClient api) => _api = api;

    [HttpGet]
    public IActionResult Login(string? returnUrl = null, bool expired = false)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToHome(User.FindFirstValue(ClaimTypes.Role));
        if (expired) ViewData["Info"] = "Your session has ended. Please log in again.";
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _api.LoginAsync(model.Email, model.Password);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            return View(model);
        }

        await SignInAsync(result.Data!);

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return LocalRedirect(model.ReturnUrl);

        return RedirectToHome(result.Data!.User.Role);
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToHome(User.FindFirstValue(ClaimTypes.Role));
        return View(new RegisterViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (model.Role == Lookups.Organiser && string.IsNullOrWhiteSpace(model.OrganiserCode))
            ModelState.AddModelError(nameof(model.OrganiserCode), "Organisers need an organiser code.");

        if (!ModelState.IsValid) return View(model);

        var registered = await _api.RegisterAsync(model);
        if (!registered.Success)
        {
            ModelState.AddModelError(string.Empty, registered.Error);
            return View(model);
        }

        // Log the new user straight in.
        var login = await _api.LoginAsync(model.Email, model.Password);
        if (!login.Success)
        {
            SetSuccess("Your account was created. Please log in.");
            return RedirectToAction(nameof(Login));
        }

        await SignInAsync(login.Data!);
        SetSuccess($"Welcome to RaceDay, {login.Data!.User.FullName}!");
        return RedirectToHome(login.Data.User.Role);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        try
        {
            await _api.LogoutAsync();   // ends the API session too
        }
        catch (ApiSessionExpiredException)
        {
            // already ended - nothing more to do
        }

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        SetSuccess("You have been logged out.");
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();

    private async Task SignInAsync(LoginResult login)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, login.User.UserID.ToString()),
            new(ClaimTypes.Name, login.User.FullName),
            new(ClaimTypes.Email, login.User.Email),
            new(ClaimTypes.Role, login.User.Role),
            new(AuthConstants.ApiSessionClaim, login.SessionCookie)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = false, ExpiresUtc = DateTimeOffset.UtcNow.AddHours(2) });
    }

    /// <summary>Each role lands on its own home page.</summary>
    private IActionResult RedirectToHome(string? role) =>
        role == Lookups.Organiser
            ? RedirectToAction("Dashboard", "Organiser")
            : RedirectToAction("Index", "Home");
}