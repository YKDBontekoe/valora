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
}
