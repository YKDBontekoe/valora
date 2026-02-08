using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Text.Json;
using Valora.Application.Common.Exceptions;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Application.Enrichment;
using Valora.Infrastructure.Enrichment;

namespace Valora.UnitTests.Services;

public class ContextReportServiceTests
{
    private readonly Mock<ILocationResolver> _locationResolver;
    private readonly Mock<ICbsNeighborhoodStatsClient> _cbsClient;
    private readonly Mock<IAmenityClient> _amenityClient;
    private readonly Mock<IAirQualityClient> _airClient;
    private readonly Mock<ILogger<ContextReportService>> _logger;
    private readonly IMemoryCache _memoryCache;

    public ContextReportServiceTests()
    {
        _locationResolver = new Mock<ILocationResolver>();
        _cbsClient = new Mock<ICbsNeighborhoodStatsClient>();
        _amenityClient = new Mock<IAmenityClient>();
        _airClient = new Mock<IAirQualityClient>();
        _logger = new Mock<ILogger<ContextReportService>>();
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
    }

    [Fact]
    public async Task BuildAsync_WithAllSourcesAvailable_ReturnsScoredReport()
    {
        var location = CreateLocation();

        _locationResolver.Setup(x => x.ResolveAsync("Damrak 1 Amsterdam", It.IsAny<CancellationToken>()))
            .ReturnsAsync(location);

        _cbsClient.Setup(x => x.GetStatsAsync(location, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NeighborhoodStatsDto("GM0363", "Gemeente", 860000, 6057, 290, 7.5, DateTimeOffset.UtcNow));

        _amenityClient.Setup(x => x.GetAmenitiesAsync(location, 1000, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AmenityStatsDto(10, 14, 11, 8, 20, 120, 100, DateTimeOffset.UtcNow));

        _airClient.Setup(x => x.GetSnapshotAsync(location, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AirQualitySnapshotDto("NL49014", "Amsterdam-Vondelpark", 900, 9.2, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));

        var service = CreateService();

        var report = await service.BuildAsync(new ContextReportRequestDto("Damrak 1 Amsterdam"));

        Assert.Equal("Damrak 1, 1012LG Amsterdam", report.Location.DisplayAddress);
        Assert.NotEmpty(report.SocialMetrics);
        Assert.NotEmpty(report.AmenityMetrics);
        Assert.NotEmpty(report.EnvironmentMetrics);
        Assert.True(report.CompositeScore > 0);
        Assert.Empty(report.Warnings);
    }

    [Fact]
    public async Task BuildAsync_WhenSomeSourcesFail_ReturnsPartialReportWithWarnings()
    {
        var location = CreateLocation();

        _locationResolver.Setup(x => x.ResolveAsync("Damrak 1 Amsterdam", It.IsAny<CancellationToken>()))
            .ReturnsAsync(location);

        _cbsClient.Setup(x => x.GetStatsAsync(location, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NeighborhoodStatsDto?)null);

        _amenityClient.Setup(x => x.GetAmenitiesAsync(location, 1000, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AmenityStatsDto(1, 2, 1, 1, 2, 450, 80, DateTimeOffset.UtcNow));

        _airClient.Setup(x => x.GetSnapshotAsync(location, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AirQualitySnapshotDto?)null);

        var service = CreateService();

        var report = await service.BuildAsync(new ContextReportRequestDto("Damrak 1 Amsterdam"));

        Assert.Empty(report.SocialMetrics);
        Assert.NotEmpty(report.AmenityMetrics);
        Assert.Empty(report.EnvironmentMetrics);
        Assert.Contains(report.Warnings, w => w.Contains("CBS neighborhood indicators", StringComparison.Ordinal));
        Assert.Contains(report.Warnings, w => w.Contains("Air quality source", StringComparison.Ordinal));
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
    public async Task BuildAsync_WhenSourceThrows_ReturnsPartialReportWithWarnings()
    {
        var location = CreateLocation();

        _locationResolver.Setup(x => x.ResolveAsync("Damrak 1 Amsterdam", It.IsAny<CancellationToken>()))
            .ReturnsAsync(location);

        _cbsClient.Setup(x => x.GetStatsAsync(location, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("CBS unavailable"));

        _amenityClient.Setup(x => x.GetAmenitiesAsync(location, 1000, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AmenityStatsDto(1, 2, 1, 1, 2, 450, 80, DateTimeOffset.UtcNow));

        _airClient.Setup(x => x.GetSnapshotAsync(location, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new JsonException("Malformed payload"));

        var service = CreateService();

        var report = await service.BuildAsync(new ContextReportRequestDto("Damrak 1 Amsterdam"));

        Assert.Empty(report.SocialMetrics);
        Assert.NotEmpty(report.AmenityMetrics);
        Assert.Empty(report.EnvironmentMetrics);
        Assert.Contains(report.Warnings, w => w.Contains("CBS neighborhood indicators", StringComparison.Ordinal));
        Assert.Contains(report.Warnings, w => w.Contains("Air quality source", StringComparison.Ordinal));
    }

    private ContextReportService CreateService()
    {
        return new ContextReportService(
            _locationResolver.Object,
            _cbsClient.Object,
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
