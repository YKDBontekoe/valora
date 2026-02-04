using Hangfire;
using Valora.Application.Common.Interfaces;
using Valora.Infrastructure.Jobs;

namespace Valora.Infrastructure.Services;

public class HangfireJobScheduler : IScraperJobScheduler
{
    private readonly IBackgroundJobClient _backgroundJobClient;

    public HangfireJobScheduler(IBackgroundJobClient backgroundJobClient)
    {
        _backgroundJobClient = backgroundJobClient;
    }

    public string EnqueueScraper(CancellationToken cancellationToken)
    {
        return _backgroundJobClient.Enqueue<FundaScraperJob>(j => j.ExecuteAsync(cancellationToken));
    }

    public string EnqueueLimitedScraper(string region, int limit, CancellationToken cancellationToken)
    {
        return _backgroundJobClient.Enqueue<FundaScraperJob>(j => j.ExecuteLimitedAsync(region, limit, cancellationToken));
    }

    public string EnqueueSeed(string region, CancellationToken cancellationToken)
    {
        return _backgroundJobClient.Enqueue<FundaSeedJob>(j => j.ExecuteAsync(region, cancellationToken));
    }
}
