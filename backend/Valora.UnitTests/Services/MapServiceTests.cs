using Valora.Domain.Entities;
using System.Text.Json;
using Moq;
using Valora.Application.Common.Exceptions;
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

    [Theory]
    [InlineData(91, 0, 52, 5)]
    [InlineData(50, 0, 49, 5)]
    [InlineData(50, 0, 51, 1)]
    public async Task GetMapAmenitiesAsync_ShouldThrowValidationException_WhenArgsInvalid(double minLat, double minLon, double maxLat, double maxLon)
    {
        await Assert.ThrowsAsync<ValidationException>(() =>
            _mapService.GetMapAmenitiesAsync(minLat, minLon, maxLat, maxLon));
    }

    [Fact]
    public async Task GetMapOverlaysAsync_PricePerSquareMeter_ShouldCalculateCorrectly()
    {
        var listingData = new List<ListingPriceData>
        {
            new ListingPriceData(500000, 100, 52.0005, 5.0005),
            new ListingPriceData(300000, 100, 52.0005, 5.0005)
        };

        _repositoryMock.Setup(x => x.GetListingsPriceDataAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(listingData);

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
        Assert.Equal(4000, result[0].MetricValue);
    }

    [Fact]
    public async Task GetMapOverlayTilesAsync_ShouldReturnRasterizedGrid()
    {
        var geoJson = JsonSerializer.SerializeToElement(new {
            type = "Polygon",
            coordinates = new[] { new[] {
                new[] { 5.0000, 52.0000 },
                new[] { 5.0100, 52.0000 },
                new[] { 5.0100, 52.0100 },
                new[] { 5.0000, 52.0100 },
                new[] { 5.0000, 52.0000 }
            }}
        });

        var dummyOverlays = new List<MapOverlayDto> {
            new MapOverlayDto("BU01", "B1", "Pop", 100, "100", geoJson)
        };

        _cbsGeoClientMock.Setup(x => x.GetNeighborhoodOverlaysAsync(
            It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(),
            It.IsAny<MapOverlayMetric>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dummyOverlays);

        double minLat = 52.0000, minLon = 5.0000;
        double maxLat = 52.0150, maxLon = 5.0150;
        double zoom = 13;

        var result = await _mapService.GetMapOverlayTilesAsync(minLat, minLon, maxLat, maxLon, zoom, MapOverlayMetric.PopulationDensity);

        Assert.NotEmpty(result);
        Assert.All(result, tile => Assert.True(tile.Value == 100));
        Assert.Equal(0.005, result[0].Size);
    }

    [Theory]
    [InlineData(13, 0.005)]
    [InlineData(11, 0.01)]
    [InlineData(9, 0.02)]
    [InlineData(7, 0.05)]
    [InlineData(6, 0.1)]
    [InlineData(2, 0.1)] // Default case
    public async Task GetMapOverlayTilesAsync_ShouldUseCorrectCellSize_ForDifferentZooms(double zoom, double expectedSize)
    {
        var geoJson = JsonSerializer.SerializeToElement(new {
            type = "Polygon",
            coordinates = new[] { new[] {
                new[] { 5.0000, 52.0000 },
                new[] { 5.1000, 52.0000 },
                new[] { 5.1000, 52.1000 },
                new[] { 5.0000, 52.1000 },
                new[] { 5.0000, 52.0000 }
            }}
        });

        var dummyOverlays = new List<MapOverlayDto> {
            new MapOverlayDto("BU01", "B1", "Pop", 100, "100", geoJson)
        };

        _cbsGeoClientMock.Setup(x => x.GetNeighborhoodOverlaysAsync(
            It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(),
            It.IsAny<MapOverlayMetric>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dummyOverlays);

        double minLat = 52.0000, minLon = 5.0000;
        double maxLat = 52.2000, maxLon = 5.2000;

        var result = await _mapService.GetMapOverlayTilesAsync(minLat, minLon, maxLat, maxLon, zoom, MapOverlayMetric.PopulationDensity);

        Assert.NotEmpty(result);
        Assert.Equal(expectedSize, result[0].Size);
    }

    [Fact]
    public async Task GetMapAmenityClustersAsync_ShouldClusterAmenities()
    {
        var amenities = new List<MapAmenityDto>
        {
            new("1", "school", "School 1", 52.0010, 5.0010),
            new("2", "school", "School 2", 52.0020, 5.0020),
            new("3", "park", "Park 1", 52.0080, 5.0080)
        };

        _amenityClientMock.Setup(x => x.GetAmenitiesInBboxAsync(
            It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(),
            It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(amenities);

        double minLat = 52.0000, minLon = 5.0000;
        double maxLat = 52.0100, maxLon = 5.0100;
        double zoom = 13;

        var result = await _mapService.GetMapAmenityClustersAsync(minLat, minLon, maxLat, maxLon, zoom);

        Assert.Equal(2, result.Count);

        var cluster1 = result.OrderByDescending(c => c.Count).First();
        Assert.Equal(2, cluster1.Count);
        Assert.Contains("school", cluster1.TypeCounts.Keys);
        Assert.Equal(2, cluster1.TypeCounts["school"]);
    }

    [Theory]
    // Max span > 2.0 cases
    [InlineData(50, 0, 53, 0)]
    [InlineData(50, 0, 50.1, 3)]
    // Invalid Order
    [InlineData(52, 0, 51, 0)] // minLat >= maxLat
    [InlineData(50, 5, 51, 4)] // minLon >= maxLon
    // NaN Cases
    [InlineData(double.NaN, 0, 50, 0)]
    [InlineData(0, double.NaN, 50, 0)]
    [InlineData(0, 0, double.NaN, 0)]
    [InlineData(0, 0, 50, double.NaN)]
    public async Task GetMapAmenityClustersAsync_ShouldThrow_WhenBboxInvalid(double minLat, double minLon, double maxLat, double maxLon)
    {
        await Assert.ThrowsAsync<ValidationException>(() =>
            _mapService.GetMapAmenityClustersAsync(minLat, minLon, maxLat, maxLon, 13));
    }

    [Theory]
    [InlineData(50, 0, 53, 0)]
    public async Task GetMapOverlayTilesAsync_ShouldThrow_WhenBboxInvalid(double minLat, double minLon, double maxLat, double maxLon)
    {
        await Assert.ThrowsAsync<ValidationException>(() =>
            _mapService.GetMapOverlayTilesAsync(minLat, minLon, maxLat, maxLon, 13, MapOverlayMetric.PopulationDensity));
    }

    [Fact]
    public async Task GetMapAmenityClustersAsync_ShouldReturnEmpty_WhenNoAmenities()
    {
        _amenityClientMock.Setup(x => x.GetAmenitiesInBboxAsync(
            It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(),
            It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MapAmenityDto>());

        var result = await _mapService.GetMapAmenityClustersAsync(52.0, 5.0, 52.1, 5.1, 13);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetMapOverlayTilesAsync_ShouldReturnEmpty_WhenNoOverlays()
    {
        _cbsGeoClientMock.Setup(x => x.GetNeighborhoodOverlaysAsync(
            It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(),
            It.IsAny<MapOverlayMetric>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MapOverlayDto>());

        var result = await _mapService.GetMapOverlayTilesAsync(52.0, 5.0, 52.1, 5.1, 13, MapOverlayMetric.PopulationDensity);
        Assert.Empty(result);
    }

    [Fact]
    public async Task CalculateAveragePriceOverlayAsync_ShouldHandleEmptyListings()
    {
        // Arrange
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

        // Act
        var result = await _mapService.GetMapOverlaysAsync(52.0, 5.0, 52.1, 5.1, MapOverlayMetric.PricePerSquareMeter);

        // Assert
        Assert.Single(result);
        Assert.Equal(0, result[0].MetricValue);
        Assert.Equal("No listing data", result[0].DisplayValue);
    }

    [Fact]
    public async Task CalculateAveragePriceOverlayAsync_ShouldFilterInvalidListings()
    {
        // Arrange
        var listingData = new List<ListingPriceData>
        {
            new ListingPriceData(null, 100, 52.0005, 5.0005), // Null price
            new ListingPriceData(300000, 0, 52.0005, 5.0005),  // 0 area
            new ListingPriceData(300000, -10, 52.0005, 5.0005)  // Negative area
        };

        _repositoryMock.Setup(x => x.GetListingsPriceDataAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(listingData);

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

        // Act
        var result = await _mapService.GetMapOverlaysAsync(52.0, 5.0, 52.1, 5.1, MapOverlayMetric.PricePerSquareMeter);

        // Assert
        Assert.Single(result);
        Assert.Equal(0, result[0].MetricValue);
        Assert.Equal("No listing data", result[0].DisplayValue);
    }

    [Fact]
    public async Task GetPropertyDetailAsync_ShouldReturnNull_WhenListingNotFound()
    {
        _repositoryMock.Setup(x => x.GetListingByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Listing?)null);

        var result = await _mapService.GetPropertyDetailAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetPropertyDetailAsync_ShouldCalculatePercentile_WhenEnoughData()
    {
        var listingId = Guid.NewGuid();
        var listing = new Listing
        {
            Id = listingId,
            FundaId = "1",
            Address = "Test",
            Price = 500000,
            LivingAreaM2 = 100, // 5000/m2
            Latitude = 52.0,
            Longitude = 5.0
        };

        _repositoryMock.Setup(x => x.GetListingByIdAsync(listingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(listing);

        var nearbyListings = new List<ListingPriceData>
        {
            new ListingPriceData(400000, 100, 52.0, 5.0), // 4000 (Cheaper)
            new ListingPriceData(600000, 100, 52.0, 5.0), // 6000 (More expensive)
            new ListingPriceData(500000, 100, 52.0, 5.0)  // 5000 (Same)
        };

        _repositoryMock.Setup(x => x.GetListingsPriceDataAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(nearbyListings);

        _amenityClientMock.Setup(x => x.GetAmenitiesInBboxAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MapAmenityDto>());

        var result = await _mapService.GetPropertyDetailAsync(listingId);

        Assert.NotNull(result);
        Assert.Equal(5000, result.PricePerM2);
        // Valid prices: 4000, 5000, 6000.
        // Current is 5000.
        // Count lower = 1 (4000).
        // Total = 3.
        // Percentile = 1/3 * 100 = 33.33...
        Assert.NotNull(result.PricePercentile);
        Assert.True(result.PricePercentile > 30 && result.PricePercentile < 35);
    }

    [Fact]
    public async Task GetMapPropertiesAsync_ShouldCallRepository()
    {
        var properties = new List<MapPropertyDto>
        {
            new MapPropertyDto(Guid.NewGuid(), 500000, 52.0, 5.0, "Sold")
        };

        _repositoryMock.Setup(x => x.GetMapPropertiesAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(properties);

        var result = await _mapService.GetMapPropertiesAsync(52.0, 5.0, 52.1, 5.1);

        Assert.Single(result);
        Assert.Equal("Sold", result[0].Status);
    }
}
