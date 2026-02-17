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
    public async Task GetRecentJobsAsync_ShouldReturnDtos()
    {
        var service = CreateService();
        var jobs = new List<BatchJob>
        {
            new BatchJob { Type = BatchJobType.CityIngestion, Target = "Amsterdam", Status = BatchJobStatus.Completed }
        };
        _jobRepositoryMock.Setup(x => x.GetRecentJobsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(jobs);

        var result = await service.GetRecentJobsAsync();

        Assert.Single(result);
        Assert.Equal("Amsterdam", result[0].Target);
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
    public async Task ProcessNextJobAsync_ShouldProcessCityIngestion_WithMultipleNeighborhoods()
    {
        var service = CreateService();
        var job = new BatchJob { Type = BatchJobType.CityIngestion, Target = "Amsterdam", Status = BatchJobStatus.Pending };
        _jobRepositoryMock.Setup(x => x.GetNextPendingJobAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        var neighborhoods = new List<NeighborhoodGeometryDto>();
        for (int i = 1; i <= 6; i++)
        {
            neighborhoods.Add(new NeighborhoodGeometryDto($"BU00{i}", $"Buurt {i}", "Buurt", 52.0 + i, 4.0 + i));
        }

        _geoClientMock.Setup(x => x.GetNeighborhoodsByMunicipalityAsync("Amsterdam", It.IsAny<CancellationToken>()))
            .ReturnsAsync(neighborhoods);

        _neighborhoodRepositoryMock.Setup(x => x.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Neighborhood?)null);

        _statsClientMock.Setup(x => x.GetStatsAsync(It.IsAny<ResolvedLocationDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NeighborhoodStatsDto("code", "type", 100, 200, 300, 400, 500, 600, 700, 800, 900, 1000, 1100, 1200, 1300, 1400, 1500, "urban", 1600, 1700, 1800, 1900, 2000, 2100, 2200, 2300, 2400, 2500, 2600, 2700, 2800, 2900, 3000, 3100, 3200, 3300, 3400, 3500, DateTimeOffset.UtcNow));

        await service.ProcessNextJobAsync();

        Assert.Equal(BatchJobStatus.Completed, job.Status);
        Assert.Equal(100, job.Progress);
        _jobRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<BatchJob>(), It.IsAny<CancellationToken>()), Times.AtLeast(3));
        _neighborhoodRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Neighborhood>(), It.IsAny<CancellationToken>()), Times.Exactly(6));
    }

    [Fact]
    public async Task ProcessNextJobAsync_ShouldHandleNullStatsAndCrime()
    {
        var service = CreateService();
        var job = new BatchJob { Type = BatchJobType.CityIngestion, Target = "Amsterdam", Status = BatchJobStatus.Pending };
        _jobRepositoryMock.Setup(x => x.GetNextPendingJobAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        var neighborhoods = new List<NeighborhoodGeometryDto>
        {
            new NeighborhoodGeometryDto("BU001", "Buurt 1", "Buurt", 52.0, 4.0)
        };

        _geoClientMock.Setup(x => x.GetNeighborhoodsByMunicipalityAsync("Amsterdam", It.IsAny<CancellationToken>()))
            .ReturnsAsync(neighborhoods);

        _neighborhoodRepositoryMock.Setup(x => x.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Neighborhood?)null);

        _statsClientMock.Setup(x => x.GetStatsAsync(It.IsAny<ResolvedLocationDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((NeighborhoodStatsDto?)null);
        _crimeClientMock.Setup(x => x.GetStatsAsync(It.IsAny<ResolvedLocationDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CrimeStatsDto?)null);

        await service.ProcessNextJobAsync();

        Assert.Equal(BatchJobStatus.Completed, job.Status);
        _neighborhoodRepositoryMock.Verify(x => x.AddAsync(It.Is<Neighborhood>(n => n.PopulationDensity == null && n.AverageWozValue == null && n.CrimeRate == null), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessNextJobAsync_ShouldHandleCancellation()
    {
        var service = CreateService();
        var job = new BatchJob { Type = BatchJobType.CityIngestion, Target = "Amsterdam", Status = BatchJobStatus.Pending };
        _jobRepositoryMock.Setup(x => x.GetNextPendingJobAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        var neighborhoods = new List<NeighborhoodGeometryDto>
        {
            new NeighborhoodGeometryDto("BU001", "Buurt 1", "Buurt", 52.0, 4.0)
        };

        _geoClientMock.Setup(x => x.GetNeighborhoodsByMunicipalityAsync("Amsterdam", It.IsAny<CancellationToken>()))
            .ReturnsAsync(neighborhoods);

        var cts = new CancellationTokenSource();
        cts.Cancel();

        await service.ProcessNextJobAsync(cts.Token);

        Assert.Equal(BatchJobStatus.Failed, job.Status);
        Assert.Contains("The operation was canceled", job.Error);
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

        await service.ProcessNextJobAsync();

        Assert.Equal(BatchJobStatus.Failed, job.Status);
        Assert.Equal("API Error", job.Error);
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
