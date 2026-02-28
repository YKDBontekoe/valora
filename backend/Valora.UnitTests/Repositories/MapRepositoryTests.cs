using Microsoft.EntityFrameworkCore;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;
using Valora.Infrastructure.Persistence.Repositories;
using Xunit;

namespace Valora.UnitTests.Repositories;

public class MapRepositoryTests
{
    private readonly ValoraDbContext _context;
    private readonly MapRepository _repository;

    public MapRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ValoraDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ValoraDbContext(options);
        _repository = new MapRepository(_context);
    }

    [Fact]
    public async Task GetCityInsightsAsync_ShouldReturnAggregatedData()
    {
        // Arrange
        _context.Properties.AddRange(
            new Property { BagId = "1", Address = "A1", City = "Amsterdam", Latitude = 52.3, Longitude = 4.9, ContextCompositeScore = 80, ContextSafetyScore = 70 },
            new Property { BagId = "2", Address = "A2", City = "Amsterdam", Latitude = 52.4, Longitude = 5.0, ContextCompositeScore = 90, ContextSafetyScore = 60 },
            new Property { BagId = "3", Address = "R1", City = "Rotterdam", Latitude = 51.9, Longitude = 4.4, ContextCompositeScore = 75, ContextSafetyScore = 80 }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetCityInsightsAsync();

        // Assert
        Assert.Equal(2, result.Count);
        
        var amsterdam = result.First(r => r.City == "Amsterdam");
        Assert.Equal(2, amsterdam.Count);
        Assert.Equal(85, amsterdam.CompositeScore);
        Assert.Equal(65, amsterdam.SafetyScore);

        var rotterdam = result.First(r => r.City == "Rotterdam");
        Assert.Equal(1, rotterdam.Count);
        Assert.Equal(75, rotterdam.CompositeScore);
    }

    [Fact]
    public async Task GetListingsPriceDataAsync_ShouldReturnEmptyList()
    {
        // Arrange
        _context.Properties.AddRange(
            new Property { BagId = "1", Address = "A1", City = "Amsterdam", Latitude = 52.3, Longitude = 4.9 },
            new Property { BagId = "2", Address = "A2", City = "Amsterdam", Latitude = 52.4, Longitude = 5.0 }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetListingsPriceDataAsync(52.0, 4.0, 53.0, 6.0);

        // Assert
        Assert.Empty(result);
    }
}
