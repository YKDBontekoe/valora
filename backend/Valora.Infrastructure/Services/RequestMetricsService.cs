using System.Collections.Concurrent;
using Valora.Application.Common.Interfaces;

namespace Valora.Infrastructure.Services;

public class RequestMetricsService : IRequestMetricsService
{
    private readonly ConcurrentQueue<long> _durations = new();
    private const int MaxSamples = 1000;

    private int _currentVersion = 1;
    private int _cachedVersion = 0;

    private long[] _sortedCache = Array.Empty<long>();
    private readonly object _sortLock = new();

    public void RecordRequestDuration(long milliseconds)
    {
        _durations.Enqueue(milliseconds);
        while (_durations.Count > MaxSamples)
        {
            _durations.TryDequeue(out _);
        }

        Interlocked.Increment(ref _currentVersion);
    }

    public double GetPercentile(double percentile)
    {
        if (_durations.IsEmpty) return 0;

        long[] sorted;
        int targetVersion = Volatile.Read(ref _currentVersion);

        if (Volatile.Read(ref _cachedVersion) != targetVersion)
        {
            lock (_sortLock)
            {
                if (Volatile.Read(ref _cachedVersion) != targetVersion)
                {
                    sorted = _durations.ToArray();
                    Array.Sort(sorted);
                    _sortedCache = sorted;
                    Volatile.Write(ref _cachedVersion, targetVersion);
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
