using System.Net;

namespace RaceDay.Web.Services;

/// <summary>The outcome of one API call. Error is always a user-friendly message.</summary>
public class ApiResponse
{
    public bool Success { get; init; }
    public string Error { get; init; } = string.Empty;
    public HttpStatusCode StatusCode { get; init; }
}

public class ApiResponse<T> : ApiResponse
{
    public T? Data { get; init; }

    public static ApiResponse<T> Ok(T? data, HttpStatusCode code = HttpStatusCode.OK) =>
        new() { Success = true, Data = data, StatusCode = code };

    public static ApiResponse<T> Fail(string error, HttpStatusCode code) =>
        new() { Success = false, Error = error, StatusCode = code };
}

/// <summary>Thrown when the API rejects a logged-in user's session (HTTP 401).</summary>
public class ApiSessionExpiredException : Exception
{
    public ApiSessionExpiredException() : base("The API session has expired.") { }
}