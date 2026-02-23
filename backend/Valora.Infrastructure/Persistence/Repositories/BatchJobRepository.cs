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

    public async Task<BatchJob?> GetNextPendingJobAsync(CancellationToken cancellationToken = default)
    {
        return await _context.BatchJobs
            .Where(x => x.Status == BatchJobStatus.Pending)
            .OrderBy(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
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
