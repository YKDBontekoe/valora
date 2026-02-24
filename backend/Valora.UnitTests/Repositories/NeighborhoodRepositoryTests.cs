using Microsoft.EntityFrameworkCore;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;
using Valora.Infrastructure.Persistence.Repositories;
using Xunit;

namespace Valora.UnitTests.Repositories;

public class NeighborhoodRepositoryTests
{
    private readonly DbContextOptions<ValoraDbContext> _options;

    public NeighborhoodRepositoryTests()
    {
        _options = new DbContextOptionsBuilder<ValoraDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task GetByCityAsync_ShouldReturnNeighborhoods_WhenCityMatchesExactly()
    {
        using var context = new ValoraDbContext(_options);
        var city = "Amsterdam";
        context.Neighborhoods.Add(new Neighborhood { Code = "A1", Name = "Centrum", City = city, Type = "Buurt" });
        context.Neighborhoods.Add(new Neighborhood { Code = "A2", Name = "West", City = "Rotterdam", Type = "Buurt" });
        await context.SaveChangesAsync();

        var repository = new NeighborhoodRepository(context);

        var result = await repository.GetByCityAsync(city);

        Assert.Single(result);
        Assert.Equal("Centrum", result[0].Name);
    }

    [Fact]
    public async Task GetByCityAsync_ShouldReturnEmpty_WhenCityCaseDoesNotMatch()
    {
        // This test confirms the new case-sensitive behavior (speed optimization)
        // Note: InMemory DB might be case-sensitive by default, matching the repository logic change.
        using var context = new ValoraDbContext(_options);
        var city = "Amsterdam";
        context.Neighborhoods.Add(new Neighborhood { Code = "A1", Name = "Centrum", City = city, Type = "Buurt" });
        await context.SaveChangesAsync();

        var repository = new NeighborhoodRepository(context);

        var result = await repository.GetByCityAsync("amsterdam"); // Lowercase

        Assert.Empty(result);
    }
}
