using Microsoft.Extensions.Logging;
using Moq;
using Valora.Application.Common.Interfaces;
using Valora.Application.Services.BatchJobs;
using Valora.Domain.Entities;
using Xunit;

namespace Valora.UnitTests.Services.BatchJobs;

public class AllCitiesIngestionJobProcessorTests
{
    private readonly Mock<IBatchJobRepository> _jobRepositoryMock = new();
    private readonly Mock<ICbsGeoClient> _geoClientMock = new();
    private readonly Mock<ILogger<AllCitiesIngestionJobProcessor>> _loggerMock = new();

    private AllCitiesIngestionJobProcessor CreateProcessor()
    {
        return new AllCitiesIngestionJobProcessor(
            _jobRepositoryMock.Object,
            _geoClientMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task ProcessAsync_ShouldQueueJobs_WhenMunicipalitiesFound()
    {
        // Arrange
        var processor = CreateProcessor();
        var job = new BatchJob { Type = BatchJobType.AllCitiesIngestion, Target = "Netherlands", Status = BatchJobStatus.Processing };
        var cities = new List<string> { "Amsterdam", "Rotterdam", "Utrecht" };

        _geoClientMock.Setup(x => x.GetAllMunicipalitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(cities);

        // Act
        await processor.ProcessAsync(job, CancellationToken.None);

        // Assert
        _jobRepositoryMock.Verify(x => x.AddAsync(It.Is<BatchJob>(j => j.Type == BatchJobType.CityIngestion && cities.Contains(j.Target)), It.IsAny<CancellationToken>()), Times.Exactly(3));
        _jobRepositoryMock.Verify(x => x.UpdateAsync(job, It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        Assert.Contains("Successfully queued 3 jobs", job.ExecutionLog);
        Assert.Contains("Queued ingestion for 3 municipalities", job.ResultSummary);
    }

    [Fact]
    public async Task ProcessAsync_ShouldHandleNoMunicipalitiesFound()
    {
        // Arrange
        var processor = CreateProcessor();
        var job = new BatchJob { Type = BatchJobType.AllCitiesIngestion, Target = "Netherlands", Status = BatchJobStatus.Processing };

        _geoClientMock.Setup(x => x.GetAllMunicipalitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        // Act
        await processor.ProcessAsync(job, CancellationToken.None);

        // Assert
        Assert.Equal("No municipalities found.", job.ResultSummary);
        _jobRepositoryMock.Verify(x => x.AddAsync(It.IsAny<BatchJob>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessAsync_ShouldHandleCancellation()
    {
        // Arrange
        var processor = CreateProcessor();
        var job = new BatchJob { Type = BatchJobType.AllCitiesIngestion, Target = "Netherlands", Status = BatchJobStatus.Processing };
        var cities = Enumerable.Range(1, 20).Select(i => $"City{i}").ToList();

        _geoClientMock.Setup(x => x.GetAllMunicipalitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(cities);

        // Simulate cancellation in DB
        _jobRepositoryMock.Setup(x => x.GetStatusAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BatchJobStatus.Failed);

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => processor.ProcessAsync(job, CancellationToken.None));
    }
}
