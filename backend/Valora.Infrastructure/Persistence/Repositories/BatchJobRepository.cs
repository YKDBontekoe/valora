using Microsoft.EntityFrameworkCore;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Domain.Entities;

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

    public async Task<List<BatchJobSummaryDto>> GetRecentJobsAsync(int limit = 10, CancellationToken cancellationToken = default)
    {
        var jobs = await _context.BatchJobs
            .OrderByDescending(x => x.CreatedAt)
            .Take(limit)
            .Select(job => new
            {
                job.Id,
                job.Type,
                job.Status,
                job.Target,
                job.Progress,
                job.Error,
                job.ResultSummary,
                job.CreatedAt,
                job.StartedAt,
                job.CompletedAt
            })
            .ToListAsync(cancellationToken);

        return jobs.Select(j => new BatchJobSummaryDto(
            j.Id,
            j.Type.ToString(),
            j.Status.ToString(),
            j.Target,
            j.Progress,
            j.Error,
            j.ResultSummary,
            j.CreatedAt,
            j.StartedAt,
            j.CompletedAt
        )).ToList();
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
