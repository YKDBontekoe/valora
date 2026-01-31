namespace Valora.Application.Common.Interfaces;

public interface IJobScheduler
{
    Task EnqueueScraperJobAsync(CancellationToken cancellationToken = default);
    Task EnqueueSeedJobAsync(string region, CancellationToken cancellationToken = default);
}
