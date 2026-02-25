using System.Collections.Concurrent;
using Valora.Application.Common.Interfaces;

namespace Valora.Infrastructure.Services;

public class RequestMetricsService : IRequestMetricsService
{
    private readonly ConcurrentQueue<long> _durations = new();
    private const int MaxSamples = 1000;

    public void RecordRequestDuration(long milliseconds)
    {
        _durations.Enqueue(milliseconds);
        while (_durations.Count > MaxSamples)
        {
            _durations.TryDequeue(out _);
        }
    }

    public double GetPercentile(double percentile)
    {
        if (_durations.IsEmpty) return 0;

        var sorted = _durations.ToArray();
        Array.Sort(sorted);

        var index = (int)Math.Ceiling(percentile / 100.0 * sorted.Length) - 1;
        if (index < 0) index = 0;
        return sorted[Math.Max(0, Math.Min(index, sorted.Length - 1))];
    }
}
