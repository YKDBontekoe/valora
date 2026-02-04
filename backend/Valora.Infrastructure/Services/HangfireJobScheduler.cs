using Hangfire;
using Valora.Application.Common.Interfaces;
using Valora.Infrastructure.Jobs;

namespace Valora.Infrastructure.Services;

public class HangfireJobScheduler : IScraperJobScheduler
{
    public string EnqueueScraper(CancellationToken cancellationToken)
    {
        return BackgroundJob.Enqueue<FundaScraperJob>(j => j.ExecuteAsync(cancellationToken));
    }

    public string EnqueueLimitedScraper(string region, int limit, CancellationToken cancellationToken)
    {
        return BackgroundJob.Enqueue<FundaScraperJob>(j => j.ExecuteLimitedAsync(region, limit, cancellationToken));
    }

    public string EnqueueSeed(string region, CancellationToken cancellationToken)
    {
        return BackgroundJob.Enqueue<FundaSeedJob>(j => j.ExecuteAsync(region, cancellationToken));
    }
}
