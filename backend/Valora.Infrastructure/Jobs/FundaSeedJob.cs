using Microsoft.Extensions.Logging;
using Valora.Application.Scraping;

namespace Valora.Infrastructure.Jobs;

/// <summary>
/// Hangfire job for initial seeding/scraping of a specific region.
/// </summary>
public class FundaSeedJob
{
    private readonly IFundaScraperService _scraperService;
    private readonly ILogger<FundaSeedJob> _logger;

    public FundaSeedJob(
        IFundaScraperService scraperService,
        ILogger<FundaSeedJob> logger)
    {
        _scraperService = scraperService;
        _logger = logger;
    }

    public async Task ExecuteAsync(string region, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("FundaSeedJob starting for region {Region}", region);

        try
        {
            // Limit to 10 as per requirements
            await _scraperService.ScrapeLimitedAsync(region, 10, cancellationToken);
            _logger.LogInformation("FundaSeedJob completed successfully for region {Region}", region);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FundaSeedJob failed for region {Region}", region);
            throw;
        }
    }
}
