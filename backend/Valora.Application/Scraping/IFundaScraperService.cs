namespace Valora.Application.Scraping;

public interface IFundaScraperService
{
    /// <summary>
    /// Scrapes configured search URLs and stores/updates listings in the database.
    /// </summary>
    Task ScrapeAndStoreAsync(CancellationToken cancellationToken = default);
}
