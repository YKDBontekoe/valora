using Microsoft.Extensions.Logging;
using Valora.Application.Common.Interfaces;
using Valora.Domain.Entities;

namespace Valora.Application.Services.BatchJobs;

public class BatchJobStateManager : IBatchJobStateManager
{
    private readonly IBatchJobRepository _jobRepository;
    private readonly ILogger<BatchJobStateManager> _logger;

    public BatchJobStateManager(IBatchJobRepository jobRepository, ILogger<BatchJobStateManager> logger)
    {
        _jobRepository = jobRepository;
        _logger = logger;
    }

    public async Task MarkJobStartedAsync(BatchJob job, CancellationToken cancellationToken = default)
    {
        // Job is typically already marked as Processing by the repository claim method,
        // but we ensure the timestamp and log are correct.
        if (job.Status != BatchJobStatus.Processing)
        {
            job.Status = BatchJobStatus.Processing;
        }

        job.StartedAt = DateTime.UtcNow;
        AppendLog(job, "Job started.");
        await _jobRepository.UpdateAsync(job, cancellationToken);
    }

    public async Task MarkJobCompletedAsync(BatchJob job, string? message = null, CancellationToken cancellationToken = default)
    {
        job.Status = BatchJobStatus.Completed;
        job.CompletedAt = DateTime.UtcNow;
        job.Progress = 100;
        AppendLog(job, message ?? "Job completed successfully.");

        await _jobRepository.UpdateAsync(job, cancellationToken);
    }

    public async Task MarkJobFailedAsync(BatchJob job, string? message = null, Exception? ex = null, CancellationToken cancellationToken = default)
    {
        job.Status = BatchJobStatus.Failed;
        job.CompletedAt = DateTime.UtcNow;

        if (ex != null)
        {
            // Do not expose raw exception details to the public job status
            job.Error = "Job failed due to an internal error.";
            _logger.LogError(ex, "Batch job {JobId} failed", job.Id);
            AppendLog(job, "Job failed due to an internal error.");
        }
        else
        {
            job.Error = message ?? "Job failed.";
            _logger.LogInformation("Batch job {JobId} cancelled/failed: {Message}", job.Id, job.Error);
            AppendLog(job, message ?? "Job failed.");
        }

        await _jobRepository.UpdateAsync(job, cancellationToken);
    }

    private static void AppendLog(BatchJob job, string message)
    {
        var entry = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] {message}";
        if (string.IsNullOrEmpty(job.ExecutionLog))
            job.ExecutionLog = entry;
        else
            job.ExecutionLog += Environment.NewLine + entry;
    }
}
