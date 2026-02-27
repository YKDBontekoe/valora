using Valora.Domain.Entities;
using Valora.Domain.Enums;

namespace Valora.Application.Services.BatchJobs;

public interface IBatchJobProcessor
{
    BatchJobType JobType { get; }
    Task ProcessAsync(BatchJob job, CancellationToken cancellationToken);
}
