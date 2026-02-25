namespace Valora.Application.Common.Interfaces;

public interface IRequestMetricsService
{
    void RecordRequestDuration(long milliseconds);
    double GetPercentile(double percentile);
}
