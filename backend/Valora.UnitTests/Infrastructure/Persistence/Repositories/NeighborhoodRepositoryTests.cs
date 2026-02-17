using Microsoft.EntityFrameworkCore;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;
using Valora.Infrastructure.Persistence.Repositories;

namespace Valora.UnitTests.Infrastructure.Persistence.Repositories;

public class NeighborhoodRepositoryTests
{
    private DbContextOptions<ValoraDbContext> CreateOptions()
    {
        return new DbContextOptionsBuilder<ValoraDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task AddAsync_ShouldAddNeighborhood()
    {
        // Arrange
        using var context = new ValoraDbContext(CreateOptions());
        var repository = new NeighborhoodRepository(context);
        var neighborhood = new Neighborhood { Code = "BU001", Name = "Test", City = "Amsterdam", Type = "Buurt" };

        // Act
        var result = await repository.AddAsync(neighborhood);

        // Assert
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(1, await context.Neighborhoods.CountAsync());
    }

    [Fact]
    public async Task GetByCodeAsync_ShouldReturnNeighborhood()
    {
        // Arrange
        using var context = new ValoraDbContext(CreateOptions());
        var repository = new NeighborhoodRepository(context);
        var neighborhood = new Neighborhood { Code = "BU001", Name = "Test", City = "Amsterdam", Type = "Buurt" };
        context.Neighborhoods.Add(neighborhood);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetByCodeAsync("BU001");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test", result.Name);
    }

    [Fact]
    public async Task GetByCityAsync_ShouldReturnNeighborhoods()
    {
        // Arrange
        using var context = new ValoraDbContext(CreateOptions());
        var repository = new NeighborhoodRepository(context);
        context.Neighborhoods.AddRange(
            new Neighborhood { Code = "BU001", Name = "N1", City = "Amsterdam", Type = "Buurt" },
            new Neighborhood { Code = "BU002", Name = "N2", City = "Amsterdam", Type = "Buurt" },
            new Neighborhood { Code = "BU003", Name = "N3", City = "Utrecht", Type = "Buurt" }
        );
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetByCityAsync("Amsterdam");

        // Assert
        Assert.Equal(2, result.Count);
    }
}
