using Microsoft.Extensions.Logging;
using Valora.Application.Scraping;

namespace Valora.Infrastructure.Jobs;

/// <summary>
/// Hangfire job wrapper for the funda scraper.
/// </summary>
public class FundaScraperJob
{
    private readonly IFundaScraperService _scraperService;
    private readonly ILogger<FundaScraperJob> _logger;

    public FundaScraperJob(
        IFundaScraperService scraperService,
        ILogger<FundaScraperJob> logger)
    {
        _scraperService = scraperService;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("FundaScraperJob starting at {Time}", DateTime.UtcNow);

        try
        {
            await _scraperService.ScrapeAndStoreAsync(cancellationToken);
            _logger.LogInformation("FundaScraperJob completed successfully at {Time}", DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FundaScraperJob failed");
            throw; // Re-throw to let Hangfire handle retries
        }
    }
}
