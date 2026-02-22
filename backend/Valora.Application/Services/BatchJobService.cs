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

    public async Task<PaginatedList<BatchJobSummaryDto>> GetJobsAsync(int pageIndex, int pageSize, string? status = null, string? type = null, CancellationToken cancellationToken = default)
    {
        var paginatedJobs = await _jobRepository.GetJobsAsync(pageIndex, pageSize, status, type, cancellationToken);
        var dtos = paginatedJobs.Items.Select(MapToSummaryDto).ToList();
        return new PaginatedList<BatchJobSummaryDto>(dtos, paginatedJobs.TotalCount, paginatedJobs.PageIndex, pageSize);
    }

    public async Task<BatchJobDto> GetJobDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var job = await _jobRepository.GetByIdAsync(id, cancellationToken);
        if (job == null) throw new KeyNotFoundException($"Job with ID {id} not found.");
        return MapToDto(job);
    }

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

    public async Task ProcessNextJobAsync(CancellationToken cancellationToken = default)
    {
        var job = await _jobRepository.GetNextPendingJobAsync(cancellationToken);
        if (job == null) return;

        await MarkJobAsStartedAsync(job, cancellationToken);

        try
        {
            await ExecuteProcessorAsync(job, cancellationToken);
            MarkJobAsCompleted(job);
        }
        catch (OperationCanceledException)
        {
            MarkJobAsCancelled(job);
        }
        catch (Exception ex)
        {
            MarkJobAsFailed(job, ex);
        }

        await _jobRepository.UpdateAsync(job, cancellationToken);
    }

    private async Task MarkJobAsStartedAsync(BatchJob job, CancellationToken cancellationToken)
    {
        job.Status = BatchJobStatus.Processing;
        job.StartedAt = DateTime.UtcNow;
        AppendLog(job, "Job started.");
        await _jobRepository.UpdateAsync(job, cancellationToken);
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

    private void MarkJobAsCompleted(BatchJob job)
    {
        // Only mark as completed if the processor didn't already set it to Failed/Cancelled
        if (job.Status == BatchJobStatus.Processing)
        {
            AppendLog(job, "Job completed successfully.");
            job.Status = BatchJobStatus.Completed;
            job.CompletedAt = DateTime.UtcNow;
            job.Progress = 100;
        }
    }

    private void MarkJobAsCancelled(BatchJob job)
    {
        _logger.LogInformation("Batch job {JobId} cancelled", job.Id);
        AppendLog(job, "Job cancelled by user.");
        job.Status = BatchJobStatus.Failed;
        job.Error = "Job cancelled by user.";
        job.CompletedAt = DateTime.UtcNow;
    }

    private void MarkJobAsFailed(BatchJob job, Exception ex)
    {
        _logger.LogError(ex, "Batch job {JobId} failed", job.Id);
        AppendLog(job, $"Job failed: {ex.Message}");
        job.Status = BatchJobStatus.Failed;
        job.Error = ex.Message;
        job.CompletedAt = DateTime.UtcNow;
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
