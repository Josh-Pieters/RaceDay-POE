using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDay.Web.Services;

namespace RaceDay.Web.Filters;

/// <summary>
/// When the API answers 401 to a logged-in user, their API session has ended.
/// Sign them out of the web app too and send them back to the login page.
/// </summary>
public class ApiSessionExpiredFilter : IAsyncExceptionFilter
{
    public async Task OnExceptionAsync(ExceptionContext context)
    {
        if (context.Exception is not ApiSessionExpiredException) return;

        await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        context.Result = new RedirectToActionResult("Login", "Account", new { expired = true });
        context.ExceptionHandled = true;
    }
}