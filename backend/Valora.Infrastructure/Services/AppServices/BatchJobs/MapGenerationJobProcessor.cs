using Microsoft.Extensions.Logging;
using Valora.Domain.Entities;
using Valora.Domain.Enums;
using Valora.Application.Common.Interfaces;

namespace Valora.Infrastructure.Services.AppServices.BatchJobs;

public class MapGenerationJobProcessor : IBatchJobProcessor
{
    private readonly ILogger<MapGenerationJobProcessor> _logger;

    public MapGenerationJobProcessor(ILogger<MapGenerationJobProcessor> logger)
    {
        _logger = logger;
    }

    public BatchJobType JobType => BatchJobType.MapGeneration;

    public Task ProcessAsync(BatchJob job, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _logger.LogInformation("Processing Map Generation Job {JobId} for target {Target}", job.Id, job.Target);

        // TODO: Implement actual map generation logic (Tracking Ticket: #598)
        _logger.LogWarning("MapGenerationJobProcessor.ProcessAsync is currently unimplemented.");
        throw new NotImplementedException("Map generation is not yet implemented.");

    }
}
