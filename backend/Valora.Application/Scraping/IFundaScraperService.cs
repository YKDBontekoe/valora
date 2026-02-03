namespace Valora.Application.Scraping;

public interface IFundaScraperService
{
    /// <summary>
    /// Scrapes configured search URLs and stores/updates listings in the database.
    /// </summary>
    Task ScrapeAndStoreAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a limited scrape for a specific region/city.
    /// </summary>
    Task ScrapeLimitedAsync(string region, int limit, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks all active listings in the database for updates (price, status, etc.) via the API.
    /// </summary>
    Task UpdateExistingListingsAsync(CancellationToken cancellationToken = default);
}
