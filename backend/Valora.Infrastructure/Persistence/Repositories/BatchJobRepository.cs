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

    public async Task<PaginatedList<BatchJob>> GetJobsAsync(int pageIndex, int pageSize, string? status = null, string? type = null, CancellationToken cancellationToken = default)
    {
        var query = _context.BatchJobs.AsQueryable();

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<BatchJobStatus>(status, true, out var statusEnum))
        {
            query = query.Where(j => j.Status == statusEnum);
        }

        if (!string.IsNullOrEmpty(type) && Enum.TryParse<BatchJobType>(type, true, out var typeEnum))
        {
            query = query.Where(j => j.Type == typeEnum);
        }

        return await query
            .OrderByDescending(x => x.CreatedAt)
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
