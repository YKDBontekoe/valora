namespace Valora.Application.DTOs;

public class ListingFilterDto
{
    public string? SearchTerm { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string? City { get; set; }
    public List<string>? Cities { get; set; }
    public int? MinBedrooms { get; set; }
    public int? MinLivingArea { get; set; }
    public int? MaxLivingArea { get; set; }
    public string? SortBy { get; set; } // "Price", "Date", "LivingArea"
    public string? SortOrder { get; set; } // "asc", "desc"
    public int? Page { get; set; }
    public int? PageSize { get; set; }
}
