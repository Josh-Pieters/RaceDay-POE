namespace RaceDay.Web.Services;

public static class AuthConstants
{
    /// <summary>Name of the cookie the API issues when a user logs in.</summary>
    public const string ApiCookieName = "RaceDay.Session";

    /// <summary>Claim in the MVC login cookie that stores the API session cookie value.</summary>
    public const string ApiSessionClaim = "ApiSession";
}
