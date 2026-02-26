using Microsoft.Extensions.Logging;
using Valora.Application.Common.Interfaces;
using Valora.Domain.Entities;

namespace Valora.Application.Services;

public class BatchJobStateManager : IBatchJobStateManager
{
    private readonly IBatchJobRepository _jobRepository;
    private readonly ILogger<BatchJobStateManager> _logger;

    public BatchJobStateManager(IBatchJobRepository jobRepository, ILogger<BatchJobStateManager> logger)
    {
        _jobRepository = jobRepository;
        _logger = logger;
    }

    public async Task UpdateJobStatusAsync(BatchJob job, BatchJobStatus newStatus, string? message = null, Exception? ex = null, CancellationToken cancellationToken = default)
    {
        job.Status = newStatus;

        if (newStatus == BatchJobStatus.Processing)
        {
            job.StartedAt = DateTime.UtcNow;
            AppendLog(job, message ?? "Job started.");
        }
        else if (newStatus == BatchJobStatus.Completed)
        {
            job.CompletedAt = DateTime.UtcNow;
            job.Progress = 100;
            AppendLog(job, message ?? "Job completed successfully.");
        }
        else if (newStatus == BatchJobStatus.Failed)
        {
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
        }

        await _jobRepository.UpdateAsync(job, cancellationToken);
    }

    public void AppendLog(BatchJob job, string message)
    {
        var entry = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] {message}";
        if (string.IsNullOrEmpty(job.ExecutionLog))
            job.ExecutionLog = entry;
        else
            job.ExecutionLog += Environment.NewLine + entry;
    }
}
