using Microsoft.Extensions.Logging;
using Valora.Application.Common.Interfaces;
using Valora.Application.Services.BatchJobs;
using Valora.Domain.Entities;
using Valora.Domain.Enums;
using Valora.Application.Common.Events;

namespace Valora.Application.Services;

public class BatchJobExecutor : IBatchJobExecutor
{
    private readonly IBatchJobRepository _jobRepository;
    private readonly IEnumerable<IBatchJobProcessor> _processors;
    private readonly ILogger<BatchJobExecutor> _logger;
    private readonly IEventDispatcher _eventDispatcher;

    public BatchJobExecutor(
        IBatchJobRepository jobRepository,
        IEnumerable<IBatchJobProcessor> processors,
        ILogger<BatchJobExecutor> logger,
        IEventDispatcher eventDispatcher)
    {
        _jobRepository = jobRepository;
        _processors = processors;
        _logger = logger;
        _eventDispatcher = eventDispatcher;
    }

    /// <summary>
    /// Picks the next pending job from the queue and executes it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Concurrency:</strong> This method relies on <see cref="IBatchJobRepository.GetNextPendingJobAsync"/> to atomically
    /// fetch and lock the next job (often using `SELECT ... FOR UPDATE SKIP LOCKED` in Postgres).
    /// </para>
    /// <para>
    /// <strong>Lifecycle:</strong>
    /// <list type="number">
    /// <item>Fetch next 'Pending' job.</item>
    /// <item>Mark as 'Processing' immediately.</item>
    /// <item>Find appropriate <see cref="IBatchJobProcessor"/>.</item>
    /// <item>Execute logic.</item>
    /// <item>Mark as 'Completed' or 'Failed'.</item>
    /// </list>
    /// </para>
    /// </remarks>
    public async Task ProcessNextJobAsync(CancellationToken cancellationToken = default)
    {
        // Repository now handles the atomic claim (find + mark processing)
        // using `CreatedAt` to enforce a Strict FIFO (First In, First Out) queue priority.
        // Recovery logic: A job stuck in "Processing" (e.g., due to pod crash)
        // must be manually cancelled to "Failed", and then retried.
        // Retrying an old job overrides its `CreatedAt` with `DateTime.UtcNow`
        // to properly requeue it at the end of the line.
        var job = await _jobRepository.GetNextPendingJobAsync(cancellationToken);
        if (job == null) return;

        // Job is already marked as Processing by the repository claim method.
        // We log it here for tracking.
        AppendLog(job, "Job started.");
        await _jobRepository.UpdateAsync(job, cancellationToken); // Save log update

        try
        {
            await ExecuteProcessorAsync(job, cancellationToken);

            // Only mark as completed if the processor didn't already set it to Failed/Cancelled
            if (job.Status == BatchJobStatus.Processing)
            {
                await UpdateJobStatusAsync(job, BatchJobStatus.Completed, "Job completed successfully.", null, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Use CancellationToken.None to ensure the status update persists even if the job token was cancelled
            await UpdateJobStatusAsync(job, BatchJobStatus.Failed, "Job cancelled by user.", null, CancellationToken.None);
        }
        catch (Exception ex)
        {
            await UpdateJobStatusAsync(job, BatchJobStatus.Failed, null, ex, cancellationToken);
        }
    }

    private async Task ExecuteProcessorAsync(BatchJob job, CancellationToken cancellationToken)
    {
        var processor = _processors.SingleOrDefault(p => p.JobType == job.Type);
        if (processor == null)
        {
            _logger.LogError("No processor found for job type {JobType}", job.Type);
            throw new InvalidOperationException("System configuration error: processor missing for job type.");
        }

        await processor.ProcessAsync(job, cancellationToken);
    }

    private async Task UpdateJobStatusAsync(BatchJob job, BatchJobStatus newStatus, string? message = null, Exception? ex = null, CancellationToken cancellationToken = default)
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

        if (newStatus == BatchJobStatus.Completed)
        {
            await _eventDispatcher.DispatchAsync(new BatchJobCompletedEvent(job.Id, job.Type, job.Target), cancellationToken);
        }
        else if (newStatus == BatchJobStatus.Failed)
        {
            await _eventDispatcher.DispatchAsync(new BatchJobFailedEvent(job.Id, job.Type, job.Target, job.Error ?? "Unknown error"), cancellationToken);
        }
    }

    private void AppendLog(BatchJob job, string message)
    {
        var entry = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] {message}";
        if (string.IsNullOrEmpty(job.ExecutionLog))
            job.ExecutionLog = entry;
        else
            job.ExecutionLog += Environment.NewLine + entry;
    }
}
