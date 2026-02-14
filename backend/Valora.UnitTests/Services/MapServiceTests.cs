using Moq;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs.Map;
using Valora.Application.Services;
using Xunit;

namespace Valora.UnitTests.Services;

public class MapServiceTests
{
    private readonly Mock<IMapRepository> _repositoryMock;
    private readonly Mock<IAmenityClient> _amenityClientMock;
    private readonly Mock<ICbsGeoClient> _cbsGeoClientMock;
    private readonly IMapService _mapService;

    public MapServiceTests()
    {
        _repositoryMock = new Mock<IMapRepository>();
        _amenityClientMock = new Mock<IAmenityClient>();
        _cbsGeoClientMock = new Mock<ICbsGeoClient>();

        _mapService = new MapService(_repositoryMock.Object, _amenityClientMock.Object, _cbsGeoClientMock.Object);
    }

    [Fact]
    public async Task GetCityInsightsAsync_ShouldReturnGroupedInsights()
    {
        // Arrange
        var insights = new List<MapCityInsightDto>
        {
            new("Utrecht", 2, 52.0, 5.0, 80, 0, 0, 0),
            new("Amsterdam", 1, 52.3, 4.9, 90, 0, 0, 0)
        };

        _repositoryMock.Setup(x => x.GetCityInsightsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(insights);

        // Act
        var result = await _mapService.GetCityInsightsAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, x => x.City == "Utrecht" && x.Count == 2);
        Assert.Contains(result, x => x.City == "Amsterdam" && x.Count == 1);
    }

    [Fact]
    public async Task GetMapAmenitiesAsync_ShouldCallClient()
    {
        // Arrange
        var amenities = new List<MapAmenityDto> { new MapAmenityDto("1", "school", "Test School", 52.0, 5.0) };
        _amenityClientMock.Setup(x => x.GetAmenitiesInBboxAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(amenities);

        // Act
        var result = await _mapService.GetMapAmenitiesAsync(52.0, 5.0, 52.1, 5.1);

        // Assert
        Assert.Single(result);
        Assert.Equal("Test School", result[0].Name);
    }

    [Fact]
    public async Task GetMapOverlaysAsync_PricePerSquareMeter_ShouldCalculateCorrectly()
    {
        // Arrange
        var listingData = new List<ListingPriceData>
        {
            new ListingPriceData(500000, 100), // 5000/m2
            new ListingPriceData(300000, 100)  // 3000/m2
        };

        _repositoryMock.Setup(x => x.GetListingsPriceDataAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(listingData);

        var dummyOverlays = new List<MapOverlayDto> {
            new MapOverlayDto("BU01", "B1", "Pop", 100, "100", default)
        };
        _cbsGeoClientMock.Setup(x => x.GetNeighborhoodOverlaysAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<MapOverlayMetric>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dummyOverlays);

        // Act
        var result = await _mapService.GetMapOverlaysAsync(52.0, 5.0, 52.1, 5.1, MapOverlayMetric.PricePerSquareMeter);

        // Assert
        Assert.Single(result);
        Assert.Equal(4000, result[0].MetricValue); // (5000+3000)/2
        Assert.StartsWith("€ ", result[0].DisplayValue);
        Assert.EndsWith(" / m²", result[0].DisplayValue);
        Assert.Contains("4000", result[0].DisplayValue.Replace(".", string.Empty).Replace(",", string.Empty));
    }
}
