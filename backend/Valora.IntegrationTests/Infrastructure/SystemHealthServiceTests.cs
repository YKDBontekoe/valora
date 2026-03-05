using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Valora.Application.Common.Interfaces;
using Valora.Domain.Entities;
using Valora.Domain.Enums;
using Valora.Infrastructure.Services;
using Xunit;

namespace Valora.IntegrationTests.Infrastructure;

public class SystemHealthServiceTests : BaseTestcontainersIntegrationTest
{
    private readonly Mock<IRequestMetricsService> _mockMetricsService;
    private readonly SystemHealthService _service;

    public SystemHealthServiceTests(TestcontainersDatabaseFixture fixture) : base(fixture)
    {
        _mockMetricsService = new Mock<IRequestMetricsService>();
        _mockMetricsService.Setup(m => m.GetPercentile(It.IsAny<double>())).Returns(10.0);
        _mockMetricsService.Setup(m => m.GetPercentile(50)).Returns(10.0);
        _mockMetricsService.Setup(m => m.GetPercentile(95)).Returns(15.0);
        _mockMetricsService.Setup(m => m.GetPercentile(99)).Returns(20.0);
        _service = new SystemHealthService(DbContext, _mockMetricsService.Object, NullLogger<SystemHealthService>.Instance);
    }

    [Fact]
    public async Task GetHealthAsync_ShouldReturnHealthyStatus_WhenDatabaseIsAccessible()
    {
        // Act
        var result = await _service.GetHealthAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Healthy", result.Status);
        Assert.True(result.Database);
        Assert.Equal(10, result.ApiLatencyP50);
        Assert.Equal(0, result.ActiveJobs);
    }

    [Fact]
    public async Task GetHealthAsync_ShouldReturnJobStats_WhenJobsExistInDatabase()
    {
        // Arrange
        var processingJob = new BatchJob
        {
            Id = Guid.NewGuid(),
            Type = BatchJobType.MapGeneration,
            Status = BatchJobStatus.Processing,
            Target = "Test"
        };
        var pendingJob = new BatchJob
        {
            Id = Guid.NewGuid(),
            Type = BatchJobType.CityIngestion,
            Status = BatchJobStatus.Pending,
            Target = "Test"
        };
        var failedJob = new BatchJob
        {
            Id = Guid.NewGuid(),
            Type = BatchJobType.AllCitiesIngestion,
            Status = BatchJobStatus.Failed,
            Target = "Test"
        };
        var completedJob = new BatchJob
        {
            Id = Guid.NewGuid(),
            Type = BatchJobType.MapGeneration,
            Status = BatchJobStatus.Completed,
            CompletedAt = DateTime.UtcNow,
            Target = "Test"
        };

        DbContext.BatchJobs.AddRange(processingJob, pendingJob, failedJob, completedJob);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetHealthAsync(CancellationToken.None);

        // Assert
        Assert.Equal("Healthy", result.Status);
        Assert.Equal(1, result.ActiveJobs);
        Assert.Equal(1, result.QueuedJobs);
        Assert.Equal(1, result.FailedJobs);
        Assert.NotNull(result.LastPipelineSuccess);
    }
}
