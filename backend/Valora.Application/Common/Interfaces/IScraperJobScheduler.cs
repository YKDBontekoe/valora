namespace Valora.Application.Common.Interfaces;

public interface IScraperJobScheduler
{
    string EnqueueScraper(CancellationToken cancellationToken);
    string EnqueueLimitedScraper(string region, int limit, CancellationToken cancellationToken);
    string EnqueueSeed(string region, CancellationToken cancellationToken);
}
