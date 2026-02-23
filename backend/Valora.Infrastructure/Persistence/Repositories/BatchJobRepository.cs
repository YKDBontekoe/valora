using Microsoft.EntityFrameworkCore;
using Valora.Application.Common.Interfaces;
using Valora.Application.Common.Models;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence.Extensions;

namespace Valora.Infrastructure.Persistence.Repositories;

public class BatchJobRepository : IBatchJobRepository
{
    private readonly ValoraDbContext _context;

    public BatchJobRepository(ValoraDbContext context)
    {
        _context = context;
    }

    public async Task<BatchJob?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.BatchJobs.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<List<BatchJob>> GetRecentJobsAsync(int limit = 10, CancellationToken cancellationToken = default)
    {
        return await _context.BatchJobs
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<PaginatedList<BatchJob>> GetJobsAsync(int pageIndex, int pageSize, BatchJobStatus? status = null, BatchJobType? type = null, string? search = null, string? sort = null, CancellationToken cancellationToken = default)
    {
        var query = _context.BatchJobs.AsNoTracking();

        if (status.HasValue)
        {
            query = query.Where(j => j.Status == status.Value);
        }

        if (type.HasValue)
        {
            query = query.Where(j => j.Type == type.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            // Note: Case-insensitivity depends on database collation.
            // For SQL Server, default collation is usually case-insensitive.
            // Removing .ToLower() to make query sargable and improve performance.
            query = query.Where(j => j.Target.Contains(search));
        }

        query = sort switch
        {
            "createdAt_asc" => query.OrderBy(x => x.CreatedAt),
            "createdAt_desc" => query.OrderByDescending(x => x.CreatedAt),
            "status_asc" => query.OrderBy(x => x.Status),
            "status_desc" => query.OrderByDescending(x => x.Status),
            "type_asc" => query.OrderBy(x => x.Type),
            "type_desc" => query.OrderByDescending(x => x.Type),
            "target_asc" => query.OrderBy(x => x.Target),
            "target_desc" => query.OrderByDescending(x => x.Target),
            _ => query.OrderByDescending(x => x.CreatedAt)
        };

        return await query
            .ToPaginatedListAsync(pageIndex, pageSize, cancellationToken);
    }

    public async Task<PaginatedList<BatchJobSummaryDto>> GetJobSummariesAsync(int pageIndex, int pageSize, BatchJobStatus? status = null, BatchJobType? type = null, string? search = null, string? sort = null, CancellationToken cancellationToken = default)
    {
        var query = _context.BatchJobs.AsNoTracking();

        if (status.HasValue)
        {
            query = query.Where(j => j.Status == status.Value);
        }

        if (type.HasValue)
        {
            query = query.Where(j => j.Type == type.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(j => j.Target.Contains(search));
        }

        query = sort switch
        {
            "createdAt_asc" => query.OrderBy(x => x.CreatedAt),
            "createdAt_desc" => query.OrderByDescending(x => x.CreatedAt),
            "status_asc" => query.OrderBy(x => x.Status),
            "status_desc" => query.OrderByDescending(x => x.Status),
            "type_asc" => query.OrderBy(x => x.Type),
            "type_desc" => query.OrderByDescending(x => x.Type),
            "target_asc" => query.OrderBy(x => x.Target),
            "target_desc" => query.OrderByDescending(x => x.Target),
            _ => query.OrderByDescending(x => x.CreatedAt)
        };

        return await query
            .Select(job => new BatchJobSummaryDto(
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
            ))
            .ToPaginatedListAsync(pageIndex, pageSize, cancellationToken);
    }

    /// <summary>
    /// Atomically claims the next pending job.
    /// Uses ExecuteUpdateAsync (EF Core 7+) to perform an atomic update on the database
    /// without needing explicit transactions or locking hints, ensuring only one worker
    /// successfully claims a specific job.
    /// </summary>
    public async Task<BatchJob?> GetNextPendingJobAsync(CancellationToken cancellationToken = default)
    {
        // 1. Fetch the ID of a potential job to process.
        // We don't lock here; we just need a candidate.
        var candidateId = await _context.BatchJobs
            .Where(x => x.Status == BatchJobStatus.Pending)
            .OrderBy(x => x.CreatedAt)
            .Select(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (candidateId == Guid.Empty)
        {
            return null;
        }

        // 2. Attempt to atomically claim this specific job.
        // This update will only succeed (return 1) if the status is still Pending at the moment of execution.
        var rowsAffected = await _context.BatchJobs
            .Where(x => x.Id == candidateId && x.Status == BatchJobStatus.Pending)
            .ExecuteUpdateAsync(s => s
                .SetProperty(j => j.Status, BatchJobStatus.Processing)
                .SetProperty(j => j.StartedAt, DateTime.UtcNow),
                cancellationToken);

        if (rowsAffected > 0)
        {
            // 3. If claimed successfully, return the full entity.
            // We fetch it again to get the updated state (and all properties).
            // Since we just claimed it, we are the owner.
            return await _context.BatchJobs.FindAsync(new object[] { candidateId }, cancellationToken);
        }

        // 4. If rowsAffected == 0, another worker claimed it in between step 1 and 2.
        // We return null to indicate "no job claimed" for this attempt.
        // The worker loop will retry immediately or after a delay.
        return null;
    }

    public async Task<BatchJob> AddAsync(BatchJob job, CancellationToken cancellationToken = default)
    {
        _context.BatchJobs.Add(job);
        await _context.SaveChangesAsync(cancellationToken);
        return job;
    }

    public async Task<BatchJobStatus?> GetStatusAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.BatchJobs
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => x.Status)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task UpdateAsync(BatchJob job, CancellationToken cancellationToken = default)
    {
        job.UpdatedAt = DateTime.UtcNow;
        _context.Entry(job).State = EntityState.Modified;
        await _context.SaveChangesAsync(cancellationToken);
    }
}
