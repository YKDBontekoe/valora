using Microsoft.Extensions.Logging;
using Valora.Application.Common.Interfaces;
using Valora.Application.Services.BatchJobs;
using Valora.Domain.Entities;

namespace Valora.Application.Services;

public class BatchJobExecutor : IBatchJobExecutor
{
    private readonly IBatchJobRepository _jobRepository;
    private readonly IBatchJobStateManager _stateManager;
    private readonly IEnumerable<IBatchJobProcessor> _processors;
    private readonly ILogger<BatchJobExecutor> _logger;

    public BatchJobExecutor(
        IBatchJobRepository jobRepository,
        IBatchJobStateManager stateManager,
        IEnumerable<IBatchJobProcessor> processors,
        ILogger<BatchJobExecutor> logger)
    {
        _jobRepository = jobRepository;
        _stateManager = stateManager;
        _processors = processors;
        _logger = logger;
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
        var job = await _jobRepository.GetNextPendingJobAsync(cancellationToken);
        if (job == null) return;

        // Job is already marked as Processing by the repository claim method.
        // We log it here for tracking.
        // Note: AppendLog only mutates the job in-memory. We must explicitly call UpdateAsync to persist it,
        // unlike UpdateJobStatusAsync which handles persistence internally.
        _stateManager.AppendLog(job, "Job started.");
        await _jobRepository.UpdateAsync(job, cancellationToken);

        try
        {
            await ExecuteProcessorAsync(job, cancellationToken);

            // Only mark as completed if the processor didn't already set it to Failed/Cancelled
            if (job.Status == BatchJobStatus.Processing)
            {
                await _stateManager.UpdateJobStatusAsync(job, BatchJobStatus.Completed, "Job completed successfully.", cancellationToken: cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Use CancellationToken.None to ensure the status update persists even if the job token was cancelled
            await _stateManager.UpdateJobStatusAsync(job, BatchJobStatus.Failed, "Job cancelled by user.", cancellationToken: CancellationToken.None);
        }
        catch (Exception ex)
        {
            // Use CancellationToken.None to ensure the failure status is persisted even if the job token was cancelled during shutdown
            await _stateManager.UpdateJobStatusAsync(job, BatchJobStatus.Failed, null, ex, CancellationToken.None);
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
}
