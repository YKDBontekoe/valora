using Microsoft.EntityFrameworkCore;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;
using Valora.Infrastructure.Persistence.Repositories;
using Xunit;

namespace Valora.UnitTests.Repositories;

public class MapRepositoryTests
{
    private readonly DbContextOptions<ValoraDbContext> _options;

    public MapRepositoryTests()
    {
        _options = new DbContextOptionsBuilder<ValoraDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task GetCityInsightsAsync_ShouldAggregateCorrectly()
    {
        using var context = new ValoraDbContext(_options);
        // Clean up
        context.Listings.RemoveRange(context.Listings);
        await context.SaveChangesAsync();

        context.Listings.AddRange(
            new Listing { FundaId = "1", Address = "A1", City = "Utrecht", Latitude = 52.0, Longitude = 5.0, ContextCompositeScore = 80, ContextSafetyScore = 70, ContextSocialScore = 60, ContextAmenitiesScore = 50 },
            new Listing { FundaId = "2", Address = "A2", City = "Utrecht", Latitude = 52.1, Longitude = 5.1, ContextCompositeScore = 60, ContextSafetyScore = 50, ContextSocialScore = 40, ContextAmenitiesScore = 30 },
            new Listing { FundaId = "3", Address = "A3", City = "Amsterdam", Latitude = 52.3, Longitude = 4.9, ContextCompositeScore = 90, ContextSafetyScore = 80, ContextSocialScore = 70, ContextAmenitiesScore = 60 }
        );
        await context.SaveChangesAsync();

        var repository = new MapRepository(context);

        var result = await repository.GetCityInsightsAsync();

        Assert.Equal(2, result.Count);

        var utrecht = result.Single(x => x.City == "Utrecht");
        Assert.Equal(2, utrecht.Count);
        Assert.Equal(70, utrecht.CompositeScore);
        Assert.Equal(60, utrecht.SafetyScore);

        var amsterdam = result.Single(x => x.City == "Amsterdam");
        Assert.Equal(1, amsterdam.Count);
        Assert.Equal(90, amsterdam.CompositeScore);
    }

    [Fact]
    public async Task GetListingsPriceDataAsync_ShouldFilterAndSelect()
    {
        using var context = new ValoraDbContext(_options);
        // Clean up
        context.Listings.RemoveRange(context.Listings);
        await context.SaveChangesAsync();

        context.Listings.AddRange(
            new Listing { FundaId = "1", Address = "In", Latitude = 52.05, Longitude = 5.05, Price = 500000, LivingAreaM2 = 100 },
            new Listing { FundaId = "2", Address = "OutLat", Latitude = 53.0, Longitude = 5.05, Price = 300000, LivingAreaM2 = 100 },
            new Listing { FundaId = "3", Address = "OutLon", Latitude = 52.05, Longitude = 6.0, Price = 300000, LivingAreaM2 = 100 },
            new Listing { FundaId = "4", Address = "NoPrice", Latitude = 52.05, Longitude = 5.05, Price = null, LivingAreaM2 = 100 },
            new Listing { FundaId = "5", Address = "NoArea", Latitude = 52.05, Longitude = 5.05, Price = 300000, LivingAreaM2 = null }
        );
        await context.SaveChangesAsync();

        var repository = new MapRepository(context);

        var result = await repository.GetListingsPriceDataAsync(52.0, 5.0, 52.1, 5.1);

        Assert.Single(result);
        Assert.Equal(500000, result[0].Price);
        Assert.Equal(100, result[0].LivingAreaM2);
    }
}
