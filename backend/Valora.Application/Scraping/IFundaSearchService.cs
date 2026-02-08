using System.ComponentModel.DataAnnotations;

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
public record FundaSearchQuery(
    [property: Required]
    [property: MaxLength(100)] string Region,
    [property: Range(0, int.MaxValue)] int? MinPrice = null,
    [property: Range(0, int.MaxValue)] int? MaxPrice = null,
    [property: Range(0, int.MaxValue)] int? MinBedrooms = null,
    [property: RegularExpression("(?i)^(buy|rent|project)$")] string OfferingType = "buy",
    [property: Range(1, 100)] int PageSize = 20,
    [property: Range(1, 10000)] int Page = 1
);

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
