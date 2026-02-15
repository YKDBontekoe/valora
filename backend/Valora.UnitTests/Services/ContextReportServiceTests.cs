using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Valora.Application.Common.Exceptions;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Application.Enrichment;
using Valora.Application.Services;

namespace Valora.UnitTests.Services;

public class ContextReportServiceTests
{
    private readonly Mock<ILocationResolver> _locationResolver;
    private readonly Mock<IContextDataProvider> _contextDataProvider;
    private readonly Mock<ICacheService> _cacheService;
    private readonly Mock<ILogger<ContextReportService>> _logger;

    public ContextReportServiceTests()
    {
        _locationResolver = new Mock<ILocationResolver>();
        _contextDataProvider = new Mock<IContextDataProvider>();
        _cacheService = new Mock<ICacheService>();
        _logger = new Mock<ILogger<ContextReportService>>();
    }

    [Fact]
    public async Task BuildAsync_WithAllSourcesAvailable_ReturnsScoredReport()
    {
        var location = CreateLocation();

        _locationResolver.Setup(x => x.ResolveAsync("Damrak 1 Amsterdam", It.IsAny<CancellationToken>()))
            .ReturnsAsync(location);

        var sourceData = new ContextSourceData(
            NeighborhoodStats: new NeighborhoodStatsDto("GM0363", "Gemeente", 1000, 6057, 290, 7.5,
                500, 500, 150, 120, 300, 250, 180, 400, 350, 250, 2.1, "Zeer sterk stedelijk", 35.0, 30.0, 20, 40, 40,
                45, 55, 20, 35, 90, 10, 80,
                1.2, 500, 1200,
                0.5, 0.3, 0.4, 0.6, 5.0,
                DateTimeOffset.UtcNow),
            CrimeStats: new CrimeStatsDto(45, 5, 3, 20, 8, null, DateTimeOffset.UtcNow),
            AmenityStats: new AmenityStatsDto(10, 14, 11, 8, 20, 120, 100, DateTimeOffset.UtcNow),
            AirQualitySnapshot: new AirQualitySnapshotDto("NL49014", "Amsterdam-Vondelpark", 900, 9.2, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow),
            Sources: new List<SourceAttributionDto>(),
            Warnings: new List<string>()
        );

        _contextDataProvider.Setup(x => x.GetSourceDataAsync(location, 1000, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sourceData);

        var service = CreateService();

        var report = await service.BuildAsync(new ContextReportRequestDto("Damrak 1 Amsterdam"));

        Assert.Equal("Damrak 1, 1012LG Amsterdam", report.Location.DisplayAddress);
        Assert.NotEmpty(report.SocialMetrics);
        Assert.NotEmpty(report.CrimeMetrics);
        Assert.NotEmpty(report.DemographicsMetrics);
        Assert.NotEmpty(report.AmenityMetrics);
        Assert.NotEmpty(report.EnvironmentMetrics);
        Assert.True(report.CompositeScore > 0);
        Assert.NotEmpty(report.CategoryScores);
        Assert.Empty(report.Warnings);
    }

    [Fact]
    public async Task BuildAsync_WhenProviderReturnsWarnings_PassesThemThrough()
    {
        var location = CreateLocation();

        _locationResolver.Setup(x => x.ResolveAsync("Damrak 1 Amsterdam", It.IsAny<CancellationToken>()))
            .ReturnsAsync(location);

        var providerWarnings = new List<string> { "Source CBS unavailable", "Source AirQuality unavailable" };
        var sourceData = new ContextSourceData(
            NeighborhoodStats: null,
            CrimeStats: null,
            AmenityStats: new AmenityStatsDto(1, 2, 1, 1, 2, 450, 80, DateTimeOffset.UtcNow),
            AirQualitySnapshot: null,
            Sources: new List<SourceAttributionDto>(),
            Warnings: providerWarnings
        );

        _contextDataProvider.Setup(x => x.GetSourceDataAsync(location, 1000, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sourceData);

        var service = CreateService();

        var report = await service.BuildAsync(new ContextReportRequestDto("Damrak 1 Amsterdam"));

        // Verify provider warnings are passed through
        foreach (var warning in providerWarnings)
        {
            Assert.Contains(warning, report.Warnings);
        }
    }

    [Fact]
    public async Task BuildAsync_WhenDataIsMissing_BuildersAddSpecificWarnings()
    {
        var location = CreateLocation();

        _locationResolver.Setup(x => x.ResolveAsync("Damrak 1 Amsterdam", It.IsAny<CancellationToken>()))
            .ReturnsAsync(location);

        // Provider returns no warnings itself, but data is null
        var sourceData = new ContextSourceData(
            NeighborhoodStats: null,
            CrimeStats: null,
            AmenityStats: new AmenityStatsDto(1, 2, 1, 1, 2, 450, 80, DateTimeOffset.UtcNow),
            AirQualitySnapshot: null,
            Sources: new List<SourceAttributionDto>(),
            Warnings: new List<string>()
        );

        _contextDataProvider.Setup(x => x.GetSourceDataAsync(location, 1000, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sourceData);

        var service = CreateService();

        var report = await service.BuildAsync(new ContextReportRequestDto("Damrak 1 Amsterdam"));

        // Verify builders added their own specific warnings
        Assert.Contains(report.Warnings, w => w.Contains("CBS neighborhood indicators were unavailable"));
        Assert.Contains(report.Warnings, w => w.Contains("CBS crime statistics were unavailable"));
        Assert.Contains(report.Warnings, w => w.Contains("Air quality source"));

        Assert.True(report.CompositeScore > 0);
    }

    [Fact]
    public async Task BuildAsync_WhenInputMissing_ThrowsValidationException()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.BuildAsync(new ContextReportRequestDto(string.Empty)));
    }

    [Fact]
    public async Task ResolveLocationAsync_ShouldCallResolver()
    {
        var location = CreateLocation();
        _locationResolver.Setup(x => x.ResolveAsync("input", It.IsAny<CancellationToken>()))
            .ReturnsAsync(location);

        var service = CreateService();
        var result = await service.ResolveLocationAsync("input");

        Assert.Equal(location, result);
    }

    [Fact]
    public async Task GetSocialMetricsAsync_ShouldReturnMetrics()
    {
        var location = CreateLocation();
        var stats = new NeighborhoodStatsDto("R1", "Region", 100, 100, 300, 5, 50, 50, 10, 10, 30, 30, 20, 40, 40, 20, 2.5, "Urban", 30, 25, 20, 40, 40, 45, 55, 20, 35, 90, 10, 80, 1.5, 600, 1000, 0.5, 0.4, 0.3, 0.2, 5, DateTimeOffset.UtcNow);
        _contextDataProvider.Setup(x => x.GetNeighborhoodStatsAsync(location, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stats);

        var service = CreateService();
        var result = await service.GetSocialMetricsAsync(location, new List<string>());

        Assert.NotEmpty(result);
        Assert.Contains(result, m => m.Key == "residents");
    }

    [Fact]
    public async Task GetSafetyMetricsAsync_ShouldReturnMetrics()
    {
        var location = CreateLocation();
        var stats = new CrimeStatsDto(50, 5, 5, 20, 20, 0, DateTimeOffset.UtcNow);
        _contextDataProvider.Setup(x => x.GetCrimeStatsAsync(location, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stats);

        var service = CreateService();
        var result = await service.GetSafetyMetricsAsync(location, new List<string>());

        Assert.NotEmpty(result);
        Assert.Contains(result, m => m.Key == "total_crimes");
    }

    [Fact]
    public async Task GetAmenityMetricsAsync_ShouldReturnMetrics()
    {
        var location = CreateLocation();
        var amen = new AmenityStatsDto(1, 1, 1, 1, 1, 100, 100, DateTimeOffset.UtcNow);
        _contextDataProvider.Setup(x => x.GetAmenityStatsAsync(location, 1000, It.IsAny<CancellationToken>()))
            .ReturnsAsync(amen);

        var service = CreateService();
        var result = await service.GetAmenityMetricsAsync(location, 1000, new List<string>());

        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task GetEnvironmentMetricsAsync_ShouldReturnMetrics()
    {
        var location = CreateLocation();
        var air = new AirQualitySnapshotDto("S1", "Station", 100, 10.5, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        _contextDataProvider.Setup(x => x.GetAirQualitySnapshotAsync(location, It.IsAny<CancellationToken>()))
            .ReturnsAsync(air);

        var service = CreateService();
        var result = await service.GetEnvironmentMetricsAsync(location, new List<string>());

        Assert.NotEmpty(result);
        Assert.Contains(result, m => m.Key == "pm25");
    }

    [Fact]
    public async Task BuildAsync_WhenRadiusClamped_AddsWarning()
    {
        var location = CreateLocation();
        _locationResolver.Setup(x => x.ResolveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(location);

        var sourceData = new ContextSourceData(
            NeighborhoodStats: null, CrimeStats: null, AmenityStats: null, AirQualitySnapshot: null,
            Sources: [], Warnings: []);

        _contextDataProvider.Setup(x => x.GetSourceDataAsync(location, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sourceData);

        var service = CreateService();
        var report = await service.BuildAsync(new ContextReportRequestDto("Damrak 1 Amsterdam", RadiusMeters: 6000));

        Assert.Contains(report.Warnings, w => w.Contains("Radius clamped"));
    }

    [Fact]
    public async Task BuildAsync_WhenCached_ReturnsCachedReport()
    {
        var location = CreateLocation();
        _locationResolver.Setup(x => x.ResolveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(location);

        var cachedReport = new ContextReportDto(location, [], [], [], [], [], [], [], 80, new Dictionary<string, double>(), [], []);

        _cacheService.Setup(x => x.TryGetValue(It.IsAny<string>(), out cachedReport))
            .Returns(true);

        var service = CreateService();
        var report = await service.BuildAsync(new ContextReportRequestDto("Damrak 1 Amsterdam"));

        Assert.Equal(80, report.CompositeScore);
        _contextDataProvider.Verify(x => x.GetSourceDataAsync(It.IsAny<ResolvedLocationDto>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private ContextReportService CreateService()
    {
        return new ContextReportService(
            _locationResolver.Object,
            _contextDataProvider.Object,
            _cacheService.Object,
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
