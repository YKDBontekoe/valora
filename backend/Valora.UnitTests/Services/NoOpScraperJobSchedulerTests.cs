using Valora.Infrastructure.Services;

namespace Valora.UnitTests.Services;

public class NoOpScraperJobSchedulerTests
{
    private readonly NoOpScraperJobScheduler _scheduler;

    public NoOpScraperJobSchedulerTests()
    {
        _scheduler = new NoOpScraperJobScheduler();
    }

    [Fact]
    public void EnqueueScraper_ShouldReturnEmptyString()
    {
        var result = _scheduler.EnqueueScraper(CancellationToken.None);
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void EnqueueLimitedScraper_ShouldReturnEmptyString()
    {
        var result = _scheduler.EnqueueLimitedScraper("Amsterdam", 10, CancellationToken.None);
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void EnqueueSeed_ShouldReturnEmptyString()
    {
        var result = _scheduler.EnqueueSeed("Amsterdam", CancellationToken.None);
        Assert.Equal(string.Empty, result);
    }
}
