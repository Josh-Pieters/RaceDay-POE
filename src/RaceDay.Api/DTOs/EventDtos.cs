using System.ComponentModel.DataAnnotations;
using RaceDay.Api.Models;

namespace RaceDay.Api.DTOs;

public class EventRequest
{
    [Required, StringLength(150)]
    public string EventName { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required]
    public DateOnly? EventDate { get; set; }

    public TimeOnly? StartTime { get; set; }

    [Required, StringLength(200)]
    public string Location { get; set; } = string.Empty;

    [StringLength(50)]
    public string? Province { get; set; }

    /// <summary>"Run", "Walk" or "Cycle".</summary>
    [Required]
    public string EventType { get; set; } = string.Empty;

    [Range(0.1, 1000)]
    public decimal DistanceKM { get; set; }
}

public class EventResponse
{
    public int EventID { get; set; }
    public int OrganiserID { get; set; }
    public string OrganiserName { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly EventDate { get; set; }
    public TimeOnly? StartTime { get; set; }
    public string Location { get; set; } = string.Empty;
    public string? Province { get; set; }
    public string EventType { get; set; } = string.Empty;
    public decimal DistanceKM { get; set; }
    public List<CategoryResponse>? Categories { get; set; }

    public static EventResponse From(Event e, bool includeCategories = false) => new()
    {
        EventID = e.EventID,
        OrganiserID = e.OrganiserID,
        OrganiserName = e.Organiser?.FullName ?? string.Empty,
        EventName = e.EventName,
        Description = e.Description,
        EventDate = e.EventDate,
        StartTime = e.StartTime,
        Location = e.Location,
        Province = e.Province,
        EventType = e.EventType,
        DistanceKM = e.DistanceKM,
        Categories = includeCategories
            ? e.Categories.OrderBy(c => c.DistanceKM).Select(c => CategoryResponse.From(c)).ToList()
            : null
    };
}
