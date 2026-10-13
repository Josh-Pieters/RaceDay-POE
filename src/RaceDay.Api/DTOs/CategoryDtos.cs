using System.ComponentModel.DataAnnotations;
using RaceDay.Api.Models;

namespace RaceDay.Api.DTOs;

public class CategoryRequest
{
    [Required, StringLength(50)]
    public string CategoryName { get; set; } = string.Empty;

    [Range(0.1, 1000)]
    public decimal DistanceKM { get; set; }

    [Range(0, 100000)]
    public decimal Price { get; set; }

    [Range(1, 1000000)]
    public int MaxParticipants { get; set; } = 500;
}

public class CategoryResponse
{
    public int CategoryID { get; set; }
    public int EventID { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal DistanceKM { get; set; }
    public decimal Price { get; set; }
    public int MaxParticipants { get; set; }

    public static CategoryResponse From(Category c) => new()
    {
        CategoryID = c.CategoryID,
        EventID = c.EventID,
        CategoryName = c.CategoryName,
        DistanceKM = c.DistanceKM,
        Price = c.Price,
        MaxParticipants = c.MaxParticipants
    };
}
