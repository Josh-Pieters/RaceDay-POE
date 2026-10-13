namespace RaceDay.Api.Models;

public static class Roles
{
    public const string Organiser = "Organiser";
    public const string Participant = "Participant";

    public static bool IsValid(string? role) => role == Organiser || role == Participant;
}

public static class EventTypes
{
    public const string Run = "Run";
    public const string Walk = "Walk";
    public const string Cycle = "Cycle";

    public static readonly string[] All = { Run, Walk, Cycle };

    /// <summary>Returns the canonical event type (case-insensitive match) or null if invalid.</summary>
    public static string? Normalise(string? value) =>
        All.FirstOrDefault(t => string.Equals(t, value?.Trim(), StringComparison.OrdinalIgnoreCase));
}

public static class EnrolmentStatuses
{
    public const string Confirmed = "Confirmed";
    public const string Cancelled = "Cancelled";
}