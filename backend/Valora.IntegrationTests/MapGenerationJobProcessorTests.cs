using Microsoft.Extensions.Logging.Abstractions;
using Valora.Application.Services.BatchJobs;
using Valora.Domain.Entities;
using Valora.Domain.Enums;
using Xunit;

namespace Valora.IntegrationTests;

public class MapGenerationJobProcessorTests
{
    [Fact]
    public void JobType_ShouldBeMapGeneration()
    {
        // Arrange
        var processor = new MapGenerationJobProcessor(NullLogger<MapGenerationJobProcessor>.Instance);

        // Act & Assert
        Assert.Equal(BatchJobType.MapGeneration, processor.JobType);
    }

    [Fact]
    public async Task ProcessAsync_ShouldSetPlaceholderResultSummary()
    {
        // Arrange
        var processor = new MapGenerationJobProcessor(NullLogger<MapGenerationJobProcessor>.Instance);
        var job = new BatchJob
        {
            Id = Guid.NewGuid(),
            Type = BatchJobType.MapGeneration,
            Target = "TestTarget",
            Status = BatchJobStatus.Processing
        };

        // Act
        await processor.ProcessAsync(job, CancellationToken.None);

        // Assert
        Assert.Equal("Map generation placeholder completed.", job.ResultSummary);
    }
}
