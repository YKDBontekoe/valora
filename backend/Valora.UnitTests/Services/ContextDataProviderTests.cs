using Microsoft.Extensions.Logging;
using Moq;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Application.Services;

namespace Valora.UnitTests.Services;

public class ContextDataProviderTests
{
    private readonly Mock<ICbsNeighborhoodStatsClient> _cbsClient;
    private readonly Mock<ICbsCrimeStatsClient> _crimeClient;
    private readonly Mock<IAmenityClient> _amenityClient;
    private readonly Mock<IAirQualityClient> _airClient;
    private readonly Mock<ILogger<ContextDataProvider>> _logger;

    public ContextDataProviderTests()
    {
        _cbsClient = new Mock<ICbsNeighborhoodStatsClient>();
        _crimeClient = new Mock<ICbsCrimeStatsClient>();
        _amenityClient = new Mock<IAmenityClient>();
        _airClient = new Mock<IAirQualityClient>();
        _logger = new Mock<ILogger<ContextDataProvider>>();
    }

    [Fact]
    public async Task GetSourceDataAsync_WhenAllSourcesSucceed_ReturnsAllData()
    {
        var location = new ResolvedLocationDto("q", "Address", 52, 4, null, null, null, null, null, null, null, null, null);

        _cbsClient.Setup(x => x.GetStatsAsync(location, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NeighborhoodStatsDto("GM", "T", 100, 100, 100, 10, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, DateTimeOffset.UtcNow));

        _crimeClient.Setup(x => x.GetStatsAsync(location, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CrimeStatsDto(10, 1, 1, 1, 1, null, DateTimeOffset.UtcNow));

        _amenityClient.Setup(x => x.GetAmenitiesAsync(location, 1000, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AmenityStatsDto(1, 1, 1, 1, 1, 100, 50, DateTimeOffset.UtcNow));

        _airClient.Setup(x => x.GetSnapshotAsync(location, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AirQualitySnapshotDto("ID", "Name", 100, 10, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));

        var provider = new ContextDataProvider(
            _cbsClient.Object,
            _crimeClient.Object,
            _amenityClient.Object,
            _airClient.Object,
            _logger.Object);

        var result = await provider.GetSourceDataAsync(location, 1000, CancellationToken.None);

        Assert.NotNull(result.NeighborhoodStats);
        Assert.NotNull(result.CrimeStats);
        Assert.NotNull(result.AmenityStats);
        Assert.NotNull(result.AirQualitySnapshot);
        // PDOK + 4 sources
        Assert.Equal(5, result.Sources.Count);
    }

    [Fact]
    public async Task GetSourceDataAsync_WhenSourceThrows_ReturnsNullForThatSourceAndLogsError()
    {
        var location = new ResolvedLocationDto("q", "Address", 52, 4, null, null, null, null, null, null, null, null, null);

        _cbsClient.Setup(x => x.GetStatsAsync(location, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("CBS down"));

        // Others succeed (or return null)
        _crimeClient.Setup(x => x.GetStatsAsync(location, It.IsAny<CancellationToken>()))
             .ReturnsAsync((CrimeStatsDto?)null);

        var provider = new ContextDataProvider(
            _cbsClient.Object,
            _crimeClient.Object,
            _amenityClient.Object,
            _airClient.Object,
            _logger.Object);

        var result = await provider.GetSourceDataAsync(location, 1000, CancellationToken.None);

        Assert.Null(result.NeighborhoodStats);
        Assert.Null(result.CrimeStats); // Expected null because setup returned null

        // Verify logger was called
        _logger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Context source CBS failed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
