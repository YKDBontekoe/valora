namespace Valora.Application.Scraping;

/// <summary>
/// Service for on-demand Funda searches with cache-through pattern.
/// Unlike the batch scraper, this fetches data in real-time based on user queries.
/// </summary>
public interface IFundaSearchService
{
    /// <summary>
    /// Search Funda for listings matching the query (region, price range, etc.)
    /// Returns cached data if fresh, otherwise fetches from Funda and caches.
    /// </summary>
    Task<FundaSearchResult> SearchAsync(FundaSearchQuery query, CancellationToken ct = default);
    
    /// <summary>
    /// Get a specific listing by Funda URL or GlobalId.
    /// Fetches from Funda if not cached or stale.
    /// </summary>
    Task<Domain.Entities.Listing?> GetByFundaUrlAsync(string fundaUrl, CancellationToken ct = default);
}

/// <summary>
/// Query parameters for Funda search.
/// </summary>
public record FundaSearchQuery
{
    /// <summary>
    /// Region/city to search, e.g. "amsterdam", "rotterdam".
    /// </summary>
    public required string Region { get; init; }
    
    /// <summary>
    /// Minimum price filter.
    /// </summary>
    public int? MinPrice { get; init; }
    
    /// <summary>
    /// Maximum price filter.
    /// </summary>
    public int? MaxPrice { get; init; }
    
    /// <summary>
    /// Minimum number of bedrooms.
    /// </summary>
    public int? MinBedrooms { get; init; }
    
    /// <summary>
    /// Type of offering: "buy", "rent", or "project".
    /// </summary>
    public string OfferingType { get; init; } = "buy";
    
    /// <summary>
    /// Number of results per page.
    /// </summary>
    public int PageSize { get; init; } = 20;
    
    /// <summary>
    /// Page number (1-indexed).
    /// </summary>
    public int Page { get; init; } = 1;
}

/// <summary>
/// Result of a Funda search operation.
/// </summary>
public record FundaSearchResult
{
    public required List<Domain.Entities.Listing> Items { get; init; }
    public required int TotalCount { get; init; }
    public required int Page { get; init; }
    public required int PageSize { get; init; }
    public required bool FromCache { get; init; }
}
