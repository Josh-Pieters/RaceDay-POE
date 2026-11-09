using System.Globalization;
using RaceDay.Web.Models.Api;

namespace RaceDay.Web.Helpers;

/// <summary>Small display helpers used by the views (formatting only, no business rules).</summary>
public static class UiHelpers
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static string Ordinal(int n)
    {
        if (n % 100 is >= 11 and <= 13) return $"{n}th";
        return (n % 10) switch
        {
            1 => $"{n}st",
            2 => $"{n}nd",
            3 => $"{n}rd",
            _ => $"{n}th"
        };
    }

    public static string Date(DateOnly date) => date.ToString("dd MMM yyyy", Inv);

    public static string Time(TimeOnly? time) => time.HasValue ? time.Value.ToString("HH:mm", Inv) : "TBC";

    public static string FinishTime(TimeOnly? time) => time.HasValue ? time.Value.ToString("HH:mm:ss", Inv) : "-";

    public static string Km(decimal km) => km.ToString("0.##", Inv) + " km";

    public static string Money(decimal amount) => amount == 0 ? "Free" : "R" + amount.ToString("0.00", Inv);

    /// <summary>e.g. "47th of 312" for a result.</summary>
    public static string Position(ApiRaceResult result)
    {
        if (!result.OverallPosition.HasValue) return "-";
        var position = Ordinal(result.OverallPosition.Value);
        return result.TotalFinishers.HasValue ? $"{position} of {result.TotalFinishers}" : position;
    }

    public static string StatusClass(string status) => status switch
    {
        "Confirmed" => "status-confirmed",
        "Pending" => "status-pending",
        "Cancelled" => "status-cancelled",
        _ => "status-other"
    };

    public static string TypeClass(string type) => type switch
    {
        "Walk" => "type-walk",
        "Cycle" => "type-cycle",
        _ => "type-run"
    };

    public static string Initials(string name)
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0) return "?";
        if (parts.Length == 1) return parts[0][..1].ToUpperInvariant();
        return (parts[0][..1] + parts[^1][..1]).ToUpperInvariant();
    }
}