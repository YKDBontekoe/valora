using Microsoft.Extensions.Logging;
using Moq;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Application.Services;
using Valora.Domain.Entities;
using Valora.Application.DTOs.Map;

namespace Valora.UnitTests.Services;

public class BatchJobServiceTests
{
    private readonly Mock<IBatchJobRepository> _jobRepositoryMock = new();
    private readonly Mock<INeighborhoodRepository> _neighborhoodRepositoryMock = new();
    private readonly Mock<ICbsGeoClient> _geoClientMock = new();
    private readonly Mock<ICbsNeighborhoodStatsClient> _statsClientMock = new();
    private readonly Mock<ICbsCrimeStatsClient> _crimeClientMock = new();
    private readonly Mock<ILogger<BatchJobService>> _loggerMock = new();

    private BatchJobService CreateService()
    {
        return new BatchJobService(
            _jobRepositoryMock.Object,
            _neighborhoodRepositoryMock.Object,
            _geoClientMock.Object,
            _statsClientMock.Object,
            _crimeClientMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task EnqueueJobAsync_ShouldAddJobAndReturnDto()
    {
        var service = CreateService();
        var type = BatchJobType.CityIngestion;
        var target = "Amsterdam";

        var result = await service.EnqueueJobAsync(type, target);

        Assert.Equal(target, result.Target);
        Assert.Equal(type.ToString(), result.Type);
        Assert.Equal(BatchJobStatus.Pending.ToString(), result.Status);
        _jobRepositoryMock.Verify(x => x.AddAsync(It.IsAny<BatchJob>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessNextJobAsync_ShouldDoNothing_WhenNoPendingJobs()
    {
        var service = CreateService();
        _jobRepositoryMock.Setup(x => x.GetNextPendingJobAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((BatchJob?)null);

        await service.ProcessNextJobAsync();

        _jobRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<BatchJob>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessNextJobAsync_ShouldProcessCityIngestion()
    {
        var service = CreateService();
        var job = new BatchJob { Type = BatchJobType.CityIngestion, Target = "Amsterdam", Status = BatchJobStatus.Pending };
        _jobRepositoryMock.Setup(x => x.GetNextPendingJobAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        var neighborhoods = new List<NeighborhoodGeometryDto>
        {
            new NeighborhoodGeometryDto("BU03630101", "Burgwallen-Oude Zijde", "Buurt", 52.37, 4.90)
        };
        _geoClientMock.Setup(x => x.GetNeighborhoodsByMunicipalityAsync("Amsterdam", It.IsAny<CancellationToken>()))
            .ReturnsAsync(neighborhoods);

        _neighborhoodRepositoryMock.Setup(x => x.GetByCodeAsync("BU03630101", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Neighborhood?)null);

        var statuses = new List<BatchJobStatus>();
        _jobRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<BatchJob>(), It.IsAny<CancellationToken>()))
            .Callback<BatchJob, CancellationToken>((j, ct) => statuses.Add(j.Status))
            .Returns(Task.CompletedTask);

        await service.ProcessNextJobAsync();

        Assert.Equal(BatchJobStatus.Completed, job.Status);
        Assert.Contains(BatchJobStatus.Processing, statuses);
        Assert.Contains(BatchJobStatus.Completed, statuses);
    }

    [Fact]
    public async Task ProcessNextJobAsync_ShouldHandleFailure()
    {
        var service = CreateService();
        var job = new BatchJob { Type = BatchJobType.CityIngestion, Target = "Amsterdam", Status = BatchJobStatus.Pending };
        _jobRepositoryMock.Setup(x => x.GetNextPendingJobAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        _geoClientMock.Setup(x => x.GetNeighborhoodsByMunicipalityAsync("Amsterdam", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("API Error"));

        var statuses = new List<BatchJobStatus>();
        _jobRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<BatchJob>(), It.IsAny<CancellationToken>()))
            .Callback<BatchJob, CancellationToken>((j, ct) => statuses.Add(j.Status))
            .Returns(Task.CompletedTask);

        await service.ProcessNextJobAsync();

        Assert.Equal(BatchJobStatus.Failed, job.Status);
        Assert.Equal("API Error", job.Error);
        Assert.Contains(BatchJobStatus.Processing, statuses);
        Assert.Contains(BatchJobStatus.Failed, statuses);
    }

    [Fact]
    public async Task ProcessNextJobAsync_ShouldHandleNoNeighborhoodsFound()
    {
        var service = CreateService();
        var job = new BatchJob { Type = BatchJobType.CityIngestion, Target = "Amsterdam", Status = BatchJobStatus.Pending };
        _jobRepositoryMock.Setup(x => x.GetNextPendingJobAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        _geoClientMock.Setup(x => x.GetNeighborhoodsByMunicipalityAsync("Amsterdam", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<NeighborhoodGeometryDto>());

        await service.ProcessNextJobAsync();

        Assert.Equal(BatchJobStatus.Completed, job.Status);
        Assert.Equal("No neighborhoods found for city.", job.ResultSummary);
    }
}
