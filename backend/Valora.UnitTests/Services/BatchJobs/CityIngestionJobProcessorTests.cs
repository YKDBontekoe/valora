using Microsoft.Extensions.Logging;
using Moq;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Application.DTOs.Map;
using Valora.Application.Services.BatchJobs;
using Valora.Domain.Entities;

namespace Valora.UnitTests.Services.BatchJobs;

public class CityIngestionJobProcessorTests
{
    private readonly Mock<IBatchJobRepository> _jobRepositoryMock = new();
    private readonly Mock<INeighborhoodRepository> _neighborhoodRepositoryMock = new();
    private readonly Mock<ICbsGeoClient> _geoClientMock = new();
    private readonly Mock<ICbsNeighborhoodStatsClient> _statsClientMock = new();
    private readonly Mock<ICbsCrimeStatsClient> _crimeClientMock = new();
    private readonly Mock<ILogger<CityIngestionJobProcessor>> _loggerMock = new();

    private CityIngestionJobProcessor CreateProcessor()
    {
        return new CityIngestionJobProcessor(
            _jobRepositoryMock.Object,
            _neighborhoodRepositoryMock.Object,
            _geoClientMock.Object,
            _statsClientMock.Object,
            _crimeClientMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task ProcessAsync_ShouldProcessCityIngestion_WithMultipleNeighborhoods()
    {
        var processor = CreateProcessor();
        var job = new BatchJob { Type = BatchJobType.CityIngestion, Target = "Amsterdam", Status = BatchJobStatus.Processing };

        var neighborhoods = new List<NeighborhoodGeometryDto>();
        for (int i = 1; i <= 6; i++)
        {
            neighborhoods.Add(new NeighborhoodGeometryDto($"BU00{i}", $"Buurt {i}", "Buurt", 52.0 + i, 4.0 + i));
        }

        _geoClientMock.Setup(x => x.GetNeighborhoodsByMunicipalityAsync("Amsterdam", It.IsAny<CancellationToken>()))
            .ReturnsAsync(neighborhoods);

        _neighborhoodRepositoryMock.Setup(x => x.GetByCityAsync("Amsterdam", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Neighborhood>());

        _statsClientMock.Setup(x => x.GetStatsAsync(It.IsAny<ResolvedLocationDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NeighborhoodStatsDto("code", "type", 100, 200, 300, 400, 500, 600, 700, 800, 900, 1000, 1100, 1200, 1300, 1400, 1500, "urban", 1600, 1700, 1800, 1900, 2000, 2100, 2200, 2300, 2400, 2500, 2600, 2700, 2800, 2900, 3000, 3100, 3200, 3300, 3400, 3500, DateTimeOffset.UtcNow));

        await processor.ProcessAsync(job, CancellationToken.None);

        Assert.Equal(100, job.Progress);
        _jobRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<BatchJob>(), It.IsAny<CancellationToken>()), Times.AtLeast(1));

        // Verify batch add was called (6 items total, batch size 10, so called once)
        _neighborhoodRepositoryMock.Verify(x => x.AddRange(It.Is<IEnumerable<Neighborhood>>(l => l.Count() == 6)), Times.Once);
        _neighborhoodRepositoryMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ProcessAsync_ShouldHandleNullStatsAndCrime()
    {
        var processor = CreateProcessor();
        var job = new BatchJob { Type = BatchJobType.CityIngestion, Target = "Amsterdam", Status = BatchJobStatus.Processing };

        var neighborhoods = new List<NeighborhoodGeometryDto>
        {
            new NeighborhoodGeometryDto("BU001", "Buurt 1", "Buurt", 52.0, 4.0)
        };

        _geoClientMock.Setup(x => x.GetNeighborhoodsByMunicipalityAsync("Amsterdam", It.IsAny<CancellationToken>()))
            .ReturnsAsync(neighborhoods);

        _neighborhoodRepositoryMock.Setup(x => x.GetByCityAsync("Amsterdam", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Neighborhood>());

        _statsClientMock.Setup(x => x.GetStatsAsync(It.IsAny<ResolvedLocationDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((NeighborhoodStatsDto?)null);
        _crimeClientMock.Setup(x => x.GetStatsAsync(It.IsAny<ResolvedLocationDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CrimeStatsDto?)null);

        await processor.ProcessAsync(job, CancellationToken.None);

        _neighborhoodRepositoryMock.Verify(x => x.AddRange(It.Is<IEnumerable<Neighborhood>>(l => l.Any(n => n.PopulationDensity == null && n.AverageWozValue == null && n.CrimeRate == null))), Times.Once);
        _neighborhoodRepositoryMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ProcessAsync_ShouldHandleCancellation()
    {
        var processor = CreateProcessor();
        var job = new BatchJob { Type = BatchJobType.CityIngestion, Target = "Amsterdam", Status = BatchJobStatus.Processing };

        var neighborhoods = new List<NeighborhoodGeometryDto>();
        // Add enough items to trigger the cancellation check (every 10 items)
        for (int i = 0; i < 20; i++)
        {
            neighborhoods.Add(new NeighborhoodGeometryDto($"BU{i}", $"Buurt {i}", "Buurt", 52.0, 4.0));
        }

        _geoClientMock.Setup(x => x.GetNeighborhoodsByMunicipalityAsync("Amsterdam", It.IsAny<CancellationToken>()))
            .ReturnsAsync(neighborhoods);

        _neighborhoodRepositoryMock.Setup(x => x.GetByCityAsync("Amsterdam", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Neighborhood>());

        // Simulate cancellation in DB
        _jobRepositoryMock.Setup(x => x.GetStatusAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BatchJobStatus.Failed);

        await Assert.ThrowsAsync<OperationCanceledException>(() => processor.ProcessAsync(job, CancellationToken.None));
    }

    [Fact]
    public async Task ProcessAsync_ShouldHandleFailure()
    {
        var processor = CreateProcessor();
        var job = new BatchJob { Type = BatchJobType.CityIngestion, Target = "Amsterdam", Status = BatchJobStatus.Processing };

        _geoClientMock.Setup(x => x.GetNeighborhoodsByMunicipalityAsync("Amsterdam", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("API Error"));

        var ex = await Assert.ThrowsAsync<ApplicationException>(() => processor.ProcessAsync(job, CancellationToken.None));
        Assert.Contains("Failed to fetch neighborhoods", ex.Message);
    }

    [Fact]
    public async Task ProcessAsync_ShouldHandleNoNeighborhoodsFound()
    {
        var processor = CreateProcessor();
        var job = new BatchJob { Type = BatchJobType.CityIngestion, Target = "Amsterdam", Status = BatchJobStatus.Processing };

        _geoClientMock.Setup(x => x.GetNeighborhoodsByMunicipalityAsync("Amsterdam", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<NeighborhoodGeometryDto>());

        await processor.ProcessAsync(job, CancellationToken.None);

        Assert.Equal("No neighborhoods found for city.", job.ResultSummary);
        _neighborhoodRepositoryMock.Verify(x => x.AddRange(It.IsAny<IEnumerable<Neighborhood>>()), Times.Never);
    }
}
