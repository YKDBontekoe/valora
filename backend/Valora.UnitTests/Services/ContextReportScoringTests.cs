using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Application.Enrichment;
using Valora.Infrastructure.Enrichment;

namespace Valora.UnitTests.Services;

public class ContextReportScoringTests
{
    private readonly Mock<ILocationResolver> _locationResolver;
    private readonly Mock<ICbsNeighborhoodStatsClient> _cbsClient;
    private readonly Mock<ICbsCrimeStatsClient> _crimeClient;
    private readonly Mock<IAmenityClient> _amenityClient;
    private readonly Mock<IAirQualityClient> _airClient;
    private readonly Mock<ILogger<ContextReportService>> _logger;
    private readonly IMemoryCache _memoryCache;
    private readonly ResolvedLocationDto _location;

    public ContextReportScoringTests()
    {
        _locationResolver = new Mock<ILocationResolver>();
        _cbsClient = new Mock<ICbsNeighborhoodStatsClient>();
        _crimeClient = new Mock<ICbsCrimeStatsClient>();
        _amenityClient = new Mock<IAmenityClient>();
        _airClient = new Mock<IAirQualityClient>();
        _logger = new Mock<ILogger<ContextReportService>>();
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _location = CreateLocation();
    }

    private void SetupDefaults(ResolvedLocationDto location)
    {
        _locationResolver.Setup(x => x.ResolveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(location);

        // Setup null returns for all clients to avoid NREs in Task.WhenAll
        _cbsClient.Setup(x => x.GetStatsAsync(location, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NeighborhoodStatsDto?)null);
        _crimeClient.Setup(x => x.GetStatsAsync(location, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CrimeStatsDto?)null);
        _amenityClient.Setup(x => x.GetAmenitiesAsync(location, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AmenityStatsDto?)null);
        _airClient.Setup(x => x.GetSnapshotAsync(location, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AirQualitySnapshotDto?)null);
    }

    [Theory]
    [InlineData(10, 100)] // Very Safe
    [InlineData(30, 85)]  // Safe
    [InlineData(45, 70)]  // Average
    [InlineData(60, 50)]  // Below Average
    [InlineData(90, 30)]  // Unsafe
    [InlineData(150, 15)] // Very Unsafe
    public async Task ScoreTotalCrime_ReturnsCorrectScore(int crimesPer1000, double expectedScore)
    {
        SetupDefaults(_location);

        _crimeClient.Setup(x => x.GetStatsAsync(_location, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CrimeStatsDto(crimesPer1000, 0, 0, 0, 0, null, DateTimeOffset.UtcNow));

        var service = CreateService();
        var report = await service.BuildAsync(new ContextReportRequestDto("test"));

        var metric = report.CrimeMetrics.Single(m => m.Key == "total_crimes");
        Assert.Equal(expectedScore, metric.Score);
    }

    [Theory]
    [InlineData(400, 65)]   // Rural
    [InlineData(1000, 85)]  // Suburban
    [InlineData(3000, 100)] // Urban Optimal
    [InlineData(5000, 70)]  // Urban Dense
    [InlineData(8000, 50)]  // Overcrowded
    public async Task ScoreDensity_ReturnsCorrectScore(int density, double expectedScore)
    {
        SetupDefaults(_location);

        _cbsClient.Setup(x => x.GetStatsAsync(_location, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NeighborhoodStatsDto("GM", "type", 100, density, 300, 5, 
            null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
            // Phase 2: Housing (7), Mobility (3), Proximity (5)
            null, null, null, null, null, null, null,
            null, null, null,
            null, null, null, null, null,
            DateTimeOffset.UtcNow));

        var service = CreateService();
        var report = await service.BuildAsync(new ContextReportRequestDto("test"));

        var metric = report.SocialMetrics.Single(m => m.Key == "population_density");
        Assert.Equal(expectedScore, metric.Score);
    }

    [Theory]
    [InlineData(0, 100)]    // No low income -> Max score
    [InlineData(5, 60)]     // 100 - (5*8) = 60
    [InlineData(10, 20)]    // 100 - (10*8) = 20
    [InlineData(15, 0)]     // 100 - (15*8) = -20 -> Clamped to 0
    public async Task ScoreLowIncome_ReturnsCorrectScore(double percent, double expectedScore)
    {
        SetupDefaults(_location);

        _cbsClient.Setup(x => x.GetStatsAsync(_location, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NeighborhoodStatsDto("GM", "type", 100, 2000, 300, percent, 
            null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
            // Phase 2: Housing (7), Mobility (3), Proximity (5)
            null, null, null, null, null, null, null,
            null, null, null,
            null, null, null, null, null,
            DateTimeOffset.UtcNow));

        var service = CreateService();
        var report = await service.BuildAsync(new ContextReportRequestDto("test"));

        var metric = report.SocialMetrics.Single(m => m.Key == "low_income_households");
        Assert.Equal(expectedScore, metric.Score);
    }

    [Theory]
    [InlineData(150, 0)]    // (150-150)/3 = 0
    [InlineData(300, 50)]   // (300-150)/3 = 50
    [InlineData(450, 100)]  // (450-150)/3 = 100
    [InlineData(600, 100)]  // > 100 -> Clamped to 100
    [InlineData(100, 0)]    // < 0 -> Clamped to 0
    public async Task ScoreWoz_ReturnsCorrectScore(double woz, double expectedScore)
    {
        SetupDefaults(_location);

        _cbsClient.Setup(x => x.GetStatsAsync(_location, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NeighborhoodStatsDto("GM", "type", 100, 2000, woz, 5, 
            null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
            // Phase 2: Housing (7), Mobility (3), Proximity (5)
            null, null, null, null, null, null, null,
            null, null, null,
            null, null, null, null, null,
            DateTimeOffset.UtcNow));

        var service = CreateService();
        var report = await service.BuildAsync(new ContextReportRequestDto("test"));

        var metric = report.SocialMetrics.Single(m => m.Key == "average_woz");
        Assert.Equal(expectedScore, metric.Score);
    }

    [Theory]
    [InlineData(200, 100)] // Very Walkable
    [InlineData(400, 85)]  // Walkable
    [InlineData(800, 70)]  // Bikeable
    [InlineData(1200, 55)] // Short Drive
    [InlineData(1800, 40)] // Drive
    [InlineData(2500, 25)] // Isolated
    public async Task ScoreAmenityProximity_ReturnsCorrectScore(double distance, double expectedScore)
    {
        SetupDefaults(_location);

        _amenityClient.Setup(x => x.GetAmenitiesAsync(_location, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AmenityStatsDto(1, 1, 1, 1, 1, distance, 50, DateTimeOffset.UtcNow));

        var service = CreateService();
        var report = await service.BuildAsync(new ContextReportRequestDto("test"));

        var metric = report.AmenityMetrics.Single(m => m.Key == "amenity_proximity");
        Assert.Equal(expectedScore, metric.Score);
    }

    [Theory]
    [InlineData(4, 100)] // Excellent
    [InlineData(8, 85)]  // Good
    [InlineData(12, 70)] // Moderate
    [InlineData(20, 50)] // Poor
    [InlineData(30, 25)] // Unhealthy
    [InlineData(40, 10)] // Hazardous
    public async Task ScorePm25_ReturnsCorrectScore(double pm25, double expectedScore)
    {
        SetupDefaults(_location);

        _airClient.Setup(x => x.GetSnapshotAsync(_location, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AirQualitySnapshotDto("ID", "Name", 100, pm25, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));

        var service = CreateService();
        var report = await service.BuildAsync(new ContextReportRequestDto("test"));

        var metric = report.EnvironmentMetrics.Single(m => m.Key == "pm25");
        Assert.Equal(expectedScore, metric.Score);
    }

    private ContextReportService CreateService()
    {
        return new ContextReportService(
            _locationResolver.Object,
            _cbsClient.Object,
            _crimeClient.Object,
            _amenityClient.Object,
            _airClient.Object,
            _memoryCache,
            Options.Create(new ContextEnrichmentOptions()),
            _logger.Object);
    }

    private static ResolvedLocationDto CreateLocation()
    {
        return new ResolvedLocationDto(
            Query: "Damrak 1 Amsterdam",
            DisplayAddress: "Damrak 1, 1012LG Amsterdam",
            Latitude: 52.37714,
            Longitude: 4.89803,
            RdX: 121691,
            RdY: 487809,
            MunicipalityCode: "GM0363",
            MunicipalityName: "Amsterdam",
            DistrictCode: "WK0363AD",
            DistrictName: "Burgwallen-Nieuwe Zijde",
            NeighborhoodCode: "BU0363AD03",
            NeighborhoodName: "Nieuwendijk-Noord",
            PostalCode: "1012LG");
    }
}
