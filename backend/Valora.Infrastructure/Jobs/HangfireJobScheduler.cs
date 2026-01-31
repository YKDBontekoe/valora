using Hangfire;
using Valora.Application.Common.Interfaces;

namespace Valora.Infrastructure.Jobs;

public class HangfireJobScheduler : IJobScheduler
{
    private readonly IBackgroundJobClient _client;

    public HangfireJobScheduler(IBackgroundJobClient client)
    {
        _client = client;
    }

    public Task EnqueueScraperJobAsync(CancellationToken cancellationToken = default)
    {
        // We use CancellationToken.None in the expression because the job runs in the background
        // and should not be linked to the HTTP request cancellation token.
        // Hangfire handles CancellationToken injection during execution.
        _client.Enqueue<FundaScraperJob>(job => job.ExecuteAsync(CancellationToken.None));
        return Task.CompletedTask;
    }

    public Task EnqueueSeedJobAsync(string region, CancellationToken cancellationToken = default)
    {
        _client.Enqueue<FundaSeedJob>(job => job.ExecuteAsync(region, CancellationToken.None));
        return Task.CompletedTask;
    }
}
