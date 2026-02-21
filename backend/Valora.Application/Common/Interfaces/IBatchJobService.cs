using Valora.Application.DTOs;
using Valora.Domain.Entities;

namespace Valora.Application.Common.Interfaces;

public interface IBatchJobService
{
    Task<BatchJobDto> EnqueueJobAsync(BatchJobType type, string target, CancellationToken cancellationToken = default);
    Task<List<BatchJobSummaryDto>> GetRecentJobsAsync(int limit = 10, CancellationToken cancellationToken = default);
    Task ProcessNextJobAsync(CancellationToken cancellationToken = default);
    Task<BatchJobDto> GetJobDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<BatchJobDto> RetryJobAsync(Guid id, CancellationToken cancellationToken = default);
    Task<BatchJobDto> CancelJobAsync(Guid id, CancellationToken cancellationToken = default);
}
