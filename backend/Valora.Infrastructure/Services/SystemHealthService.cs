using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;

namespace Valora.Infrastructure.Services;

public class SystemHealthService : ISystemHealthService
{
    private readonly ValoraDbContext _db;
    private readonly IRequestMetricsService _metricsService;
    private readonly ILogger<SystemHealthService> _logger;

    public SystemHealthService(ValoraDbContext db, IRequestMetricsService metricsService, ILogger<SystemHealthService> logger)
    {
        _db = db;
        _metricsService = metricsService;
        _logger = logger;
    }

    public async Task<SystemHealthDto> GetHealthAsync(CancellationToken ct)
    {
        try
        {
            var canConnect = await _db.Database.CanConnectAsync(ct);

            int activeJobs = 0;
            int queuedJobs = 0;
            int failedJobs = 0;
            DateTime? lastPipelineSuccess = null;

            if (canConnect)
            {
                var jobStats = await _db.BatchJobs
                    .GroupBy(j => j.Status)
                    .Select(g => new { Status = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Status, x => x.Count, ct);

                activeJobs = jobStats.TryGetValue(BatchJobStatus.Processing, out var processing) ? processing : 0;
                queuedJobs = jobStats.TryGetValue(BatchJobStatus.Pending, out var pending) ? pending : 0;
                failedJobs = jobStats.TryGetValue(BatchJobStatus.Failed, out var failed) ? failed : 0;

                lastPipelineSuccess = await _db.BatchJobs
                    .Where(j => j.Status == BatchJobStatus.Completed)
                    .OrderByDescending(j => j.CompletedAt)
                    .Select(j => j.CompletedAt)
                    .FirstOrDefaultAsync(ct);
            }

            var p50 = _metricsService.GetPercentile(50);
            var p95 = _metricsService.GetPercentile(95);
            var p99 = _metricsService.GetPercentile(99);

            return new SystemHealthDto
            {
                Status = canConnect ? "Healthy" : "Unhealthy",
                Database = canConnect,
                ApiLatency = (int)p50,
                ApiLatencyP50 = (int)p50,
                ApiLatencyP95 = (int)p95,
                ApiLatencyP99 = (int)p99,
                ActiveJobs = activeJobs,
                QueuedJobs = queuedJobs,
                FailedJobs = failedJobs,
                LastPipelineSuccess = lastPipelineSuccess,
                Timestamp = DateTime.UtcNow
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            return new SystemHealthDto
            {
                Status = "Unhealthy",
                Database = false,
                ApiLatency = 0,
                ApiLatencyP50 = 0,
                ApiLatencyP95 = 0,
                ApiLatencyP99 = 0,
                ActiveJobs = 0,
                QueuedJobs = 0,
                FailedJobs = 0,
                LastPipelineSuccess = null,
                Timestamp = DateTime.UtcNow,
                Error = "Critical system failure: " + ex.Message
            };
        }
    }
}
