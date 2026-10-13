using System.Security.Claims;

namespace RaceDay.Api.Data;

public static class UserContextExtensions
{
    /// <summary>Reads the logged-in user's id from the session cookie's claims.</summary>
    public static int GetUserId(this ClaimsPrincipal principal) =>
        int.Parse(principal.FindFirst(ClaimTypes.NameIdentifier)!.Value);
}
