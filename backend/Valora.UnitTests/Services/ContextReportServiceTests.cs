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
    public async Task BuildAsync_WhenSomeSourcesFail_ReturnsPartialReportWithWarnings()
    {
        var location = CreateLocation();

        _locationResolver.Setup(x => x.ResolveAsync("Damrak 1 Amsterdam", It.IsAny<CancellationToken>()))
            .ReturnsAsync(location);

        var warnings = new List<string> { "CBS failed", "Crime failed", "Air failed" };
        var sourceData = new ContextSourceData(
            NeighborhoodStats: null,
            CrimeStats: null,
            AmenityStats: new AmenityStatsDto(1, 2, 1, 1, 2, 450, 80, DateTimeOffset.UtcNow),
            AirQualitySnapshot: null,
            Sources: new List<SourceAttributionDto>(),
            Warnings: warnings
        );

        _contextDataProvider.Setup(x => x.GetSourceDataAsync(location, 1000, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sourceData);

        var service = CreateService();

        var report = await service.BuildAsync(new ContextReportRequestDto("Damrak 1 Amsterdam"));

        Assert.Empty(report.SocialMetrics);
        Assert.Empty(report.CrimeMetrics);
        Assert.Empty(report.DemographicsMetrics);
        Assert.NotEmpty(report.AmenityMetrics);
        Assert.Empty(report.EnvironmentMetrics);
        // We expect warnings from the provider plus potentially one from radius clamping if applicable (not here)
        foreach (var warning in warnings)
        {
            Assert.Contains(warning, report.Warnings);
        }

        // Verify that metric builders added their own warnings
        Assert.Contains(report.Warnings, w => w.Contains("CBS"));

        Assert.True(report.CompositeScore > 0);
    }

    [Fact]
    public async Task BuildAsync_WhenInputMissing_ThrowsValidationException()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.BuildAsync(new ContextReportRequestDto(string.Empty)));
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
