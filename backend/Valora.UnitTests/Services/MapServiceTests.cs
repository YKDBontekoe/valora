using Microsoft.EntityFrameworkCore;
using Moq;
using Valora.Application.Common.Interfaces;
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
            new Listing { FundaId = "1", Address = "A1", City = "Utrecht", Latitude = 52.0, Longitude = 5.0, ContextCompositeScore = 80, ContextSafetyScore = 90, ContextSocialScore = 70, ContextAmenitiesScore = 80 },
            new Listing { FundaId = "2", Address = "A2", City = "Utrecht", Latitude = 52.1, Longitude = 5.1, ContextCompositeScore = 60, ContextSafetyScore = 70, ContextSocialScore = 50, ContextAmenitiesScore = 60 },
            new Listing { FundaId = "3", Address = "A3", City = "Amsterdam", Latitude = 52.3, Longitude = 4.9, ContextCompositeScore = 90, ContextSafetyScore = 80, ContextSocialScore = 90, ContextAmenitiesScore = 100 }
        };

        _context.Listings.AddRange(listings);
        await _context.SaveChangesAsync();

        // Act
        var result = await _mapService.GetCityInsightsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);

        var utrecht = result.FirstOrDefault(x => x.City == "Utrecht");
        Assert.NotNull(utrecht);
        Assert.Equal(2, utrecht.Count);
        Assert.Equal(70, utrecht.CompositeScore); // (80+60)/2
        Assert.Equal(80, utrecht.SafetyScore);    // (90+70)/2
        Assert.Equal(52.05, utrecht.Latitude, 2); // (52.0+52.1)/2

        var amsterdam = result.FirstOrDefault(x => x.City == "Amsterdam");
        Assert.NotNull(amsterdam);
        Assert.Equal(1, amsterdam.Count);
        Assert.Equal(90, amsterdam.CompositeScore);
    }

    [Fact]
    public async Task GetCityInsightsAsync_ShouldIgnoreListingsWithoutCityOrCoordinates()
    {
        // Arrange
        var listings = new List<Listing>
        {
            new Listing { FundaId = "1", Address = "A1", City = null, Latitude = 52.0, Longitude = 5.0 }, // No City
            new Listing { FundaId = "2", Address = "A2", City = "Utrecht", Latitude = null, Longitude = 5.0 }, // No Lat
            new Listing { FundaId = "3", Address = "A3", City = "Utrecht", Latitude = 52.0, Longitude = null }, // No Lon
            new Listing { FundaId = "4", Address = "A4", City = "Valid", Latitude = 52.0, Longitude = 5.0, ContextCompositeScore = 100 }
        };

        _context.Listings.AddRange(listings);
        await _context.SaveChangesAsync();

        // Act
        var result = await _mapService.GetCityInsightsAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal("Valid", result[0].City);
    }

    [Fact]
    public async Task GetCityInsightsAsync_ShouldReturnEmptyList_WhenNoListingsExist()
    {
        // Act
        var result = await _mapService.GetCityInsightsAsync();

        // Assert
        Assert.Empty(result);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
