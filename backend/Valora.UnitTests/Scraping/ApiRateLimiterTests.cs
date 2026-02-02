using Microsoft.Extensions.Options;
using Valora.Application.Scraping;
using Valora.Infrastructure.Scraping;

namespace Valora.UnitTests.Scraping;

public class ApiRateLimiterTests
{
    [Fact]
    public async Task WaitAsync_ShouldAdvanceTime_WhenCallsAreTooFrequent()
    {
        var timeProvider = new TestTimeProvider(DateTimeOffset.UnixEpoch);
        var options = Options.Create(new ScraperOptions
        {
            MaxApiCallsPerMinute = 10
        });

        var limiter = new ApiRateLimiter(options, timeProvider);

        await limiter.WaitAsync();
        var afterFirst = timeProvider.GetUtcNow();

        await limiter.WaitAsync();
        var afterSecond = timeProvider.GetUtcNow();

        Assert.Equal(afterFirst, DateTimeOffset.UnixEpoch);
        Assert.Equal(afterFirst + TimeSpan.FromSeconds(6), afterSecond);
    }

    private sealed class TestTimeProvider : TimeProvider
    {
        private DateTimeOffset _utcNow;

        public TestTimeProvider(DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow()
        {
            return _utcNow;
        }

        public override Task Delay(TimeSpan delay, CancellationToken cancellationToken = default)
        {
            if (delay > TimeSpan.Zero)
            {
                _utcNow = _utcNow.Add(delay);
            }

            return Task.CompletedTask;
        }
    }
}
