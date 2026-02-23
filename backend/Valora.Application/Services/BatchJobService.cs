using Microsoft.Extensions.Logging;
using Valora.Application.Common.Interfaces;
using Valora.Application.Common.Models;
using Valora.Application.DTOs;
using Valora.Application.Services.BatchJobs;
using Valora.Domain.Entities;

namespace Valora.Application.Services;

public class BatchJobService : IBatchJobService
{
    private readonly IBatchJobRepository _jobRepository;
    private readonly IEnumerable<IBatchJobProcessor> _processors;
    private readonly ILogger<BatchJobService> _logger;

    public BatchJobService(
        IBatchJobRepository jobRepository,
        IEnumerable<IBatchJobProcessor> processors,
        ILogger<BatchJobService> logger)
    {
        _jobRepository = jobRepository;
        _processors = processors;
        _logger = logger;
    }

    public async Task<BatchJobDto> EnqueueJobAsync(BatchJobType type, string target, CancellationToken cancellationToken = default)
    {
        var job = new BatchJob
        {
            Type = type,
            Target = target,
            Status = BatchJobStatus.Pending,
            Progress = 0
        };

        await _jobRepository.AddAsync(job, cancellationToken);

        return MapToDto(job);
    }

    public async Task<List<BatchJobSummaryDto>> GetRecentJobsAsync(int limit = 10, CancellationToken cancellationToken = default)
    {
        var jobs = await _jobRepository.GetRecentJobsAsync(limit, cancellationToken);
        return jobs.Select(MapToSummaryDto).ToList();
    }

    public async Task<PaginatedList<BatchJobSummaryDto>> GetJobsAsync(int pageIndex, int pageSize, string? status = null, string? type = null, string? search = null, string? sort = null, CancellationToken cancellationToken = default)
    {
        BatchJobStatus? statusEnum = null;
        if (!string.IsNullOrEmpty(status))
        {
            if (Enum.TryParse<BatchJobStatus>(status, true, out var parsedStatus))
            {
                statusEnum = parsedStatus;
            }
            else
            {
                 // Throwing ArgumentException for invalid input which will be mapped to 400 Bad Request by middleware
                 throw new ArgumentException($"Invalid status: {status}");
            }
        }

        BatchJobType? typeEnum = null;
        if (!string.IsNullOrEmpty(type))
        {
            if (Enum.TryParse<BatchJobType>(type, true, out var parsedType))
            {
                typeEnum = parsedType;
            }
            else
            {
                throw new ArgumentException($"Invalid job type: {type}");
            }
        }

        var paginatedJobs = await _jobRepository.GetJobsAsync(pageIndex, pageSize, statusEnum, typeEnum, search, sort, cancellationToken);
        var dtos = paginatedJobs.Items.Select(MapToSummaryDto).ToList();
        return new PaginatedList<BatchJobSummaryDto>(dtos, paginatedJobs.TotalCount, paginatedJobs.PageIndex, pageSize);
    }

    public async Task<BatchJobDto> GetJobDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var job = await _jobRepository.GetByIdAsync(id, cancellationToken);
        if (job == null) throw new KeyNotFoundException($"Job with ID {id} not found.");
        return MapToDto(job);
    }

    /// <summary>
    /// Resets a failed or completed job to 'Pending' so it can be picked up again by a processor.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is useful for transient failures (e.g., API timeout) or when a job was cancelled by mistake.
    /// </para>
    /// <para>
    /// <strong>State Transition:</strong> Failed/Completed -> Pending.<br/>
    /// <strong>Queue Position:</strong> The `CreatedAt` timestamp is updated to <see cref="DateTime.UtcNow"/> to move it to the end of the FIFO queue.
    /// </para>
    /// </remarks>
    public async Task<BatchJobDto> RetryJobAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var job = await _jobRepository.GetByIdAsync(id, cancellationToken);
        if (job == null) throw new KeyNotFoundException($"Job with ID {id} not found.");

        if (job.Status != BatchJobStatus.Failed && job.Status != BatchJobStatus.Completed)
        {
             throw new InvalidOperationException("Only failed or completed jobs can be retried.");
        }

        job.Status = BatchJobStatus.Pending;
        job.Progress = 0;
        job.Error = null;
        job.ResultSummary = null;
        job.ExecutionLog = null;
        job.StartedAt = null;
        job.CompletedAt = null;
        // Fix: Reset CreatedAt to move to end of queue (FIFO)
        job.CreatedAt = DateTime.UtcNow;

        _logger.LogInformation("Job {JobId} retried by admin", id);
        await _jobRepository.UpdateAsync(job, cancellationToken);

        return MapToDto(job);
    }

    /// <summary>
    /// Marks a pending or processing job as failed with a cancellation message.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>State Transition:</strong> Pending/Processing -> Failed.<br/>
    /// Note: If the job is currently processing, this method updates the database status, but the running thread
    /// might continue until it checks the cancellation token or completes. The processor should ideally
    /// check the database status periodically if it's a long-running operation.
    /// </para>
    /// </remarks>
    public async Task<BatchJobDto> CancelJobAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var job = await _jobRepository.GetByIdAsync(id, cancellationToken);
        if (job == null) throw new KeyNotFoundException($"Job with ID {id} not found.");

        if (job.Status == BatchJobStatus.Completed || job.Status == BatchJobStatus.Failed)
        {
            throw new InvalidOperationException("Cannot cancel a completed or failed job.");
        }

        job.Status = BatchJobStatus.Failed;
        job.Error = "Job cancelled by user.";
        job.CompletedAt = DateTime.UtcNow;

        _logger.LogInformation("Job {JobId} cancelled by admin", id);
        await _jobRepository.UpdateAsync(job, cancellationToken);

        return MapToDto(job);
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
        var job = await _jobRepository.GetNextPendingJobAsync(cancellationToken);
        if (job == null) return;

        await UpdateJobStatusAsync(job, BatchJobStatus.Processing, "Job started.", cancellationToken: cancellationToken);

        try
        {
            await ExecuteProcessorAsync(job, cancellationToken);

            // Only mark as completed if the processor didn't already set it to Failed/Cancelled
            if (job.Status == BatchJobStatus.Processing)
            {
                await UpdateJobStatusAsync(job, BatchJobStatus.Completed, "Job completed successfully.", cancellationToken: cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            await UpdateJobStatusAsync(job, BatchJobStatus.Failed, "Job cancelled by user.", cancellationToken: cancellationToken);
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
            job.Error = ex?.Message ?? message ?? "Job failed.";

            if (ex != null)
            {
                _logger.LogError(ex, "Batch job {JobId} failed", job.Id);
                AppendLog(job, $"Job failed: {ex.Message}");
            }
            else
            {
                _logger.LogInformation("Batch job {JobId} cancelled/failed: {Message}", job.Id, job.Error);
                AppendLog(job, message ?? "Job failed.");
            }
        }

        await _jobRepository.UpdateAsync(job, cancellationToken);
    }

    private void AppendLog(BatchJob job, string message)
    {
        var entry = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] {message}";
        if (string.IsNullOrEmpty(job.ExecutionLog))
            job.ExecutionLog = entry;
        else
            job.ExecutionLog += Environment.NewLine + entry;
    }

    private static BatchJobSummaryDto MapToSummaryDto(BatchJob job) => new(
        job.Id,
        job.Type.ToString(),
        job.Status.ToString(),
        job.Target,
        job.Progress,
        job.Error,
        job.ResultSummary,
        job.CreatedAt,
        job.StartedAt,
        job.CompletedAt
    );

    private static BatchJobDto MapToDto(BatchJob job) => new(
        job.Id,
        job.Type.ToString(),
        job.Status.ToString(),
        job.Target,
        job.Progress,
        job.Error,
        job.ResultSummary, job.ExecutionLog,
        job.CreatedAt,
        job.StartedAt,
        job.CompletedAt
    );
}
