using Valora.Application.DTOs;
using Valora.Domain.Entities;

namespace Valora.Application.Common.Interfaces;

public record BatchJobDto(
    Guid Id,
    string Type,
    string Status,
    string Target,
    int Progress,
    string? Error,
    string? ResultSummary,
    string? ExecutionLog,
    DateTime CreatedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt
);

public interface IBatchJobService
{
    Task<BatchJobDto> EnqueueJobAsync(BatchJobType type, string target, CancellationToken cancellationToken = default);
    Task<List<BatchJobDto>> GetRecentJobsAsync(int limit = 10, CancellationToken cancellationToken = default);
    Task ProcessNextJobAsync(CancellationToken cancellationToken = default);
    Task<BatchJobDto> GetJobDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<BatchJobDto> RetryJobAsync(Guid id, CancellationToken cancellationToken = default);
    Task<BatchJobDto> CancelJobAsync(Guid id, CancellationToken cancellationToken = default);
}
