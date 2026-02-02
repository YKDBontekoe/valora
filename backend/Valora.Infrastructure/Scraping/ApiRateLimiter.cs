using Microsoft.Extensions.Options;
using Valora.Application.Common.Interfaces;
using Valora.Application.Scraping;

namespace Valora.Infrastructure.Scraping;

public class ApiRateLimiter : IApiRateLimiter
{
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _minInterval;
    private readonly SemaphoreSlim _mutex = new(1, 1);
    private DateTimeOffset _lastCallUtc = DateTimeOffset.MinValue;

    public ApiRateLimiter(IOptions<ScraperOptions> options, TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
        var maxCallsPerMinute = Math.Max(1, options.Value.MaxApiCallsPerMinute);
        _minInterval = TimeSpan.FromMinutes(1d / maxCallsPerMinute);
    }

    public async Task WaitAsync(CancellationToken cancellationToken = default)
    {
        await _mutex.WaitAsync(cancellationToken);
        try
        {
            var now = _timeProvider.GetUtcNow();
            var earliest = _lastCallUtc + _minInterval;
            if (now < earliest)
            {
                var delay = earliest - now;
                if (delay > TimeSpan.Zero)
                {
                    await _timeProvider.Delay(delay, cancellationToken);
                }
            }

            _lastCallUtc = _timeProvider.GetUtcNow();
        }
        finally
        {
            _mutex.Release();
        }
    }
}
