using Microsoft.Extensions.Logging;
using Valora.Domain.Entities;

namespace Valora.Application.Services.BatchJobs;

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

        // Placeholder implementation
        job.ResultSummary = "Map generation placeholder completed.";

        return Task.CompletedTask;
    }
}
