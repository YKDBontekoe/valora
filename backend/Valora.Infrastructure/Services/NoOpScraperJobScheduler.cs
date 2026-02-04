using Valora.Application.Common.Interfaces;

namespace Valora.Infrastructure.Services;

public class NoOpScraperJobScheduler : IScraperJobScheduler
{
    public string EnqueueLimitedScraper(string region, int limit, CancellationToken cancellationToken)
    {
        return string.Empty;
    }

    public string EnqueueScraper(CancellationToken cancellationToken)
    {
        return string.Empty;
    }

    public string EnqueueSeed(string region, CancellationToken cancellationToken)
    {
        return string.Empty;
    }
}
