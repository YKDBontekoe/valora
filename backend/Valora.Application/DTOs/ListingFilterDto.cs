namespace Valora.Application.DTOs;

public class ListingFilterDto
{
    public string? SearchTerm { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string? City { get; set; }
    public string? SortBy { get; set; } // "Price", "Date", "LivingArea"
    public string? SortOrder { get; set; } // "asc", "desc"
    public int? Page { get; set; }
    public int? PageSize { get; set; }

    public bool Validate(out string error)
    {
        error = string.Empty;
        if (Page.HasValue && Page.Value < 1)
        {
            error = "Page must be greater than 0.";
            return false;
        }
        if (PageSize.HasValue && (PageSize.Value < 1 || PageSize.Value > 100))
        {
            error = "PageSize must be between 1 and 100.";
            return false;
        }
        if (!string.IsNullOrEmpty(SortOrder) && SortOrder.ToLower() != "asc" && SortOrder.ToLower() != "desc")
        {
            error = "SortOrder must be 'asc' or 'desc'.";
            return false;
        }
        return true;
    }
}
