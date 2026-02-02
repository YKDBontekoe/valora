using System.ComponentModel.DataAnnotations;

namespace Valora.Application.DTOs;

public class ListingFilterDto
{
    public string? SearchTerm { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? MinPrice { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? MaxPrice { get; set; }

    public string? City { get; set; }

    [Range(0, int.MaxValue)]
    public int? MinBedrooms { get; set; }

    [Range(0, int.MaxValue)]
    public int? MinLivingArea { get; set; }

    [Range(0, int.MaxValue)]
    public int? MaxLivingArea { get; set; }

    public string? SortBy { get; set; } // "Price", "Date", "LivingArea"
    public string? SortOrder { get; set; } // "asc", "desc"

    [Range(1, int.MaxValue)]
    public int? Page { get; set; }

    [Range(1, 100)]
    public int? PageSize { get; set; }
}
