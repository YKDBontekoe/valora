using Microsoft.Extensions.Logging;
using Valora.Application.Scraping;

namespace Valora.Infrastructure.Jobs;

/// <summary>
/// Hangfire job for updating existing listings (Price, Status).
/// </summary>
public class FundaUpdateJob
{
    private readonly IFundaScraperService _scraperService;
    private readonly ILogger<FundaUpdateJob> _logger;

    public FundaUpdateJob(
        IFundaScraperService scraperService,
        ILogger<FundaUpdateJob> logger)
    {
        _scraperService = scraperService;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("FundaUpdateJob starting at {Time}", DateTime.UtcNow);

        try
        {
            // Execute the update logic to check for price/status changes across all active listings
            await _scraperService.UpdateExistingListingsAsync(cancellationToken);
            _logger.LogInformation("FundaUpdateJob completed successfully at {Time}", DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FundaUpdateJob failed");
            throw; // Re-throw to let Hangfire handle retries
        }
    }
}
