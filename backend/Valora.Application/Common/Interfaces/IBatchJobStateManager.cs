using Valora.Domain.Entities;

namespace Valora.Application.Common.Interfaces;

public interface IBatchJobStateManager
{
    Task UpdateJobStatusAsync(BatchJob job, BatchJobStatus newStatus, string? message = null, Exception? ex = null, CancellationToken cancellationToken = default);
    void AppendLog(BatchJob job, string message);
}
