using Microsoft.EntityFrameworkCore;
using Moq;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs.Map;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;
using Valora.Infrastructure.Services;

namespace Valora.UnitTests.Services;

public class MapServiceTests : IDisposable
{
    private readonly ValoraDbContext _context;
    private readonly Mock<IAmenityClient> _amenityClientMock;
    private readonly Mock<ICbsGeoClient> _cbsGeoClientMock;
    private readonly IMapService _mapService;

    public MapServiceTests()
    {
        var options = new DbContextOptionsBuilder<ValoraDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ValoraDbContext(options);
        _amenityClientMock = new Mock<IAmenityClient>();
        _cbsGeoClientMock = new Mock<ICbsGeoClient>();

        _mapService = new MapService(_context, _amenityClientMock.Object, _cbsGeoClientMock.Object);
    }

    [Fact]
    public async Task GetCityInsightsAsync_ShouldReturnGroupedInsights()
    {
        // Arrange
        var listings = new List<Listing>
        {
            new Listing { FundaId = "1", Address = "A1", City = "Utrecht", Latitude = 52.0, Longitude = 5.0, ContextCompositeScore = 80 },
            new Listing { FundaId = "2", Address = "A2", City = "Utrecht", Latitude = 52.1, Longitude = 5.1, ContextCompositeScore = 60 },
            new Listing { FundaId = "3", Address = "A3", City = "Amsterdam", Latitude = 52.3, Longitude = 4.9, ContextCompositeScore = 90 }
        };

        _context.Listings.AddRange(listings);
        await _context.SaveChangesAsync();

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
        var listings = new List<Listing>
        {
            new Listing { FundaId = "1", Address = "A1", Latitude = 52.05, Longitude = 5.05, Price = 500000, LivingAreaM2 = 100 }, // 5000/m2
            new Listing { FundaId = "2", Address = "A2", Latitude = 52.06, Longitude = 5.06, Price = 300000, LivingAreaM2 = 100 }  // 3000/m2
        };
        _context.Listings.AddRange(listings);
        await _context.SaveChangesAsync();

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
        Assert.Contains("€ 4,000 / m²", result[0].DisplayValue);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
