using System.Collections.Concurrent;
using Valora.Application.Common.Interfaces;

namespace Valora.Infrastructure.Services;

public class RequestMetricsService : IRequestMetricsService
{
    private readonly ConcurrentQueue<long> _durations = new();
    private const int MaxSamples = 1000;

    private volatile bool _isDirty = true;
    private long[] _sortedCache = Array.Empty<long>();
    private readonly object _sortLock = new();

    public void RecordRequestDuration(long milliseconds)
    {
        _durations.Enqueue(milliseconds);
        while (_durations.Count > MaxSamples)
        {
            _durations.TryDequeue(out _);
        }
        _isDirty = true;
    }

    public double GetPercentile(double percentile)
    {
        if (_durations.IsEmpty) return 0;

        long[] sorted;
        if (_isDirty)
        {
            lock (_sortLock)
            {
                if (_isDirty)
                {
                    sorted = _durations.ToArray();
                    Array.Sort(sorted);
                    _sortedCache = sorted;
                    _isDirty = false;
                }
                else
                {
                    sorted = _sortedCache;
                }
            }
        }
        else
        {
            sorted = _sortedCache;
        }

        if (sorted.Length == 0) return 0;

        var index = (int)Math.Ceiling(percentile / 100.0 * sorted.Length) - 1;
        if (index < 0) index = 0;
        return sorted[Math.Max(0, Math.Min(index, sorted.Length - 1))];
    }
}
