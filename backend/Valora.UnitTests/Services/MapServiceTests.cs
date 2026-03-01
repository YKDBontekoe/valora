using System.Text.Json;
using Moq;
using Valora.Application.Common.Exceptions;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs.Map;
using Valora.Application.Services;
using Xunit;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Valora.UnitTests.Services;

public class MapServiceTests
{
    private readonly Mock<IMapRepository> _repositoryMock;
    private readonly Mock<IAmenityClient> _amenityClientMock;
    private readonly Mock<ICbsGeoClient> _cbsGeoClientMock;
    private readonly IMemoryCache _memoryCache;
    private readonly Mock<ILogger<MapService>> _loggerMock;
    private readonly IMapService _mapService;

    public MapServiceTests()
    {
        _repositoryMock = new Mock<IMapRepository>();
        _amenityClientMock = new Mock<IAmenityClient>();
        _cbsGeoClientMock = new Mock<ICbsGeoClient>();
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _loggerMock = new Mock<ILogger<MapService>>();

        _mapService = new MapService(_repositoryMock.Object, _amenityClientMock.Object, _cbsGeoClientMock.Object, _memoryCache, _loggerMock.Object);
    }

    [Fact]
    public async Task GetCityInsightsAsync_ShouldReturnGroupedInsights()
    {
        var insights = new List<MapCityInsightDto>
        {
            new("Utrecht", 2, 52.0, 5.0, 80, 0, 0, 0),
            new("Amsterdam", 1, 52.3, 4.9, 90, 0, 0, 0)
        };

        _repositoryMock.Setup(x => x.GetCityInsightsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(insights);

        var result = await _mapService.GetCityInsightsAsync();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, x => x.City == "Utrecht" && x.Count == 2);
    }

    [Fact]
    public async Task GetMapAmenitiesAsync_ShouldCallClient()
    {
        var amenities = new List<MapAmenityDto> { new MapAmenityDto("1", "school", "Test School", 52.0, 5.0) };
        _amenityClientMock.Setup(x => x.GetAmenitiesInBboxAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(amenities);

        var result = await _mapService.GetMapAmenitiesAsync(52.0, 5.0, 52.1, 5.1);

        Assert.Single(result);
        Assert.Equal("Test School", result[0].Name);
    }

    [Fact]
    public async Task GetMapOverlaysAsync_PricePerSquareMeter_ShouldHandleNoData()
    {
        _repositoryMock.Setup(x => x.GetListingsPriceDataAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ListingPriceData>());

        var geoJson = JsonSerializer.SerializeToElement(new {
            type = "Polygon",
            coordinates = new[] { new[] {
                new[] { 5.0000, 52.0000 },
                new[] { 5.0010, 52.0000 },
                new[] { 5.0010, 52.0010 },
                new[] { 5.0000, 52.0010 },
                new[] { 5.0000, 52.0000 }
            }}
        });

        var dummyOverlays = new List<MapOverlayDto> {
            new MapOverlayDto("BU01", "B1", "Pop", 100, "100", geoJson)
        };
        _cbsGeoClientMock.Setup(x => x.GetNeighborhoodOverlaysAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<MapOverlayMetric>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dummyOverlays);

        var result = await _mapService.GetMapOverlaysAsync(52.0, 5.0, 52.1, 5.1, MapOverlayMetric.PricePerSquareMeter);

        Assert.Single(result);
        Assert.Equal(0, result[0].MetricValue);
    }
}
