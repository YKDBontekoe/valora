using Valora.Application.Common.Models;
using Valora.Application.DTOs;
using Valora.Domain.Enums;

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

public record BatchJobSummaryDto(
    Guid Id,
    BatchJobType Type,
    BatchJobStatus Status,
    string Target,
    int Progress,
    string? Error,
    string? ResultSummary,
    DateTime CreatedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt
);

public interface IBatchJobService
{
    Task<BatchJobDto> EnqueueJobAsync(BatchJobType type, string target, CancellationToken cancellationToken = default);
    Task<List<BatchJobSummaryDto>> GetRecentJobsAsync(int limit = 10, CancellationToken cancellationToken = default);
    Task<PaginatedList<BatchJobSummaryDto>> GetJobsAsync(int pageIndex, int pageSize, string? status = null, string? type = null, string? search = null, string? sort = null, CancellationToken cancellationToken = default);
    Task<BatchJobDto> GetJobDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<BatchJobDto> RetryJobAsync(Guid id, CancellationToken cancellationToken = default);
    Task<BatchJobDto> CancelJobAsync(Guid id, CancellationToken cancellationToken = default);
}
