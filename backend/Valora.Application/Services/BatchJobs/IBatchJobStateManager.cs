using Valora.Domain.Entities;

namespace Valora.Application.Services.BatchJobs;

public interface IBatchJobStateManager
{
    Task MarkJobStartedAsync(BatchJob job, CancellationToken cancellationToken = default);
    Task MarkJobCompletedAsync(BatchJob job, string? message = null, CancellationToken cancellationToken = default);
    Task MarkJobFailedAsync(BatchJob job, string? message = null, Exception? ex = null, CancellationToken cancellationToken = default);
}
