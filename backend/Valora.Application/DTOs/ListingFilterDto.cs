using System.ComponentModel.DataAnnotations;

namespace Valora.Application.DTOs;

public class ListingFilterDto
{
    [MaxLength(100)]
    public string? SearchTerm { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }
    public int? MinBedrooms { get; set; }
    public int? MinLivingArea { get; set; }
    public int? MaxLivingArea { get; set; }

    [RegularExpression("(?i)^(Price|Date|LivingArea)$", ErrorMessage = "Invalid SortBy value.")]
    public string? SortBy { get; set; } // "Price", "Date", "LivingArea"

    [RegularExpression("(?i)^(asc|desc)$", ErrorMessage = "Invalid SortOrder value.")]
    public string? SortOrder { get; set; } // "asc", "desc"

    [Range(1, 10000)]
    public int? Page { get; set; }

    [Range(1, 100)]
    public int? PageSize { get; set; }
}
