using Valora.Domain.Entities;

namespace Valora.Application.Common.Interfaces;

public interface IBatchJobRepository
{
    Task<BatchJob?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<BatchJob>> GetRecentJobsAsync(int limit = 10, CancellationToken cancellationToken = default);
    Task<BatchJob?> GetNextPendingJobAsync(CancellationToken cancellationToken = default);
    Task<BatchJob> AddAsync(BatchJob job, CancellationToken cancellationToken = default);
    Task UpdateAsync(BatchJob job, CancellationToken cancellationToken = default);
}
