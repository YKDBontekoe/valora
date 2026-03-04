using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Valora.Application.Common.Interfaces;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;
using Xunit;

namespace Valora.IntegrationTests;

[Collection("TestDatabase")]
public class NeighborhoodRepositoryTests
{
    private readonly TestDatabaseFixture _fixture;

    public NeighborhoodRepositoryTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetByCodeAsync_ShouldReturnNeighborhood_WhenExists()
    {
        // Arrange
        using var scope = _fixture.Factory!.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<INeighborhoodRepository>();

        var neighborhood = new Neighborhood { Code = "TEST_GET_01", Name = "Test Get 1", City = "GetCity", Latitude = 1, Longitude = 1, Type = "Buurt" };
        await repository.AddAsync(neighborhood);

        // Act
        var result = await repository.GetByCodeAsync("TEST_GET_01");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("TEST_GET_01", result.Code);
    }

    [Fact]
    public async Task GetByCodeAsync_ShouldReturnNull_WhenNotExists()
    {
        // Arrange
        using var scope = _fixture.Factory!.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<INeighborhoodRepository>();

        // Act
        var result = await repository.GetByCodeAsync("NON_EXISTING");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByCityAsync_ShouldReturnNeighborhoods_ForGivenCity()
    {
        // Arrange
        using var scope = _fixture.Factory!.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<INeighborhoodRepository>();

        var city = "CityMatchTest";
        var n1 = new Neighborhood { Code = "CITY_TEST_01", Name = "N1", City = city, Latitude = 1, Longitude = 1, Type = "Buurt" };
        var n2 = new Neighborhood { Code = "CITY_TEST_02", Name = "N2", City = city, Latitude = 2, Longitude = 2, Type = "Buurt" };
        var n3 = new Neighborhood { Code = "OTHER_TEST_01", Name = "N3", City = "OtherCity", Latitude = 3, Longitude = 3, Type = "Buurt" };

        repository.AddRange(new[] { n1, n2, n3 });
        await repository.SaveChangesAsync();

        // Act
        var results = await repository.GetByCityAsync(city);

        // Assert
        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Equal(city, r.City));
    }

    [Fact]
    public async Task GetDatasetStatusAsync_ShouldReturnGroupedCityStatus()
    {
        // Arrange
        using var scope = _fixture.Factory!.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<INeighborhoodRepository>();
        var context = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();

        var date1 = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var date2 = new DateTime(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        var cityAlpha = $"AlphaCity_{Guid.NewGuid()}";
        var cityBeta = $"BetaCity_{Guid.NewGuid()}";

        var n1 = new Neighborhood { Code = $"STAT_01_{Guid.NewGuid()}", Name = "Stat1", City = cityAlpha, Latitude = 1, Longitude = 1, Type = "Buurt", LastUpdated = date1 };
        var n2 = new Neighborhood { Code = $"STAT_02_{Guid.NewGuid()}", Name = "Stat2", City = cityAlpha, Latitude = 2, Longitude = 2, Type = "Buurt", LastUpdated = date2 };
        var n3 = new Neighborhood { Code = $"STAT_03_{Guid.NewGuid()}", Name = "Stat3", City = cityBeta, Latitude = 3, Longitude = 3, Type = "Buurt", LastUpdated = date1 };

        repository.AddRange(new[] { n1, n2, n3 });
        await repository.SaveChangesAsync();

        // Act
        var results = await repository.GetDatasetStatusAsync();

        // Assert
        Assert.True(results.Count >= 2); // Other tests might have added data

        var alphaStats = results.First(r => r.City == cityAlpha);
        Assert.Equal(2, alphaStats.NeighborhoodCount);
        Assert.Equal(date2, alphaStats.LastUpdated);

        var betaStats = results.First(r => r.City == cityBeta);
        Assert.Equal(1, betaStats.NeighborhoodCount);
        Assert.Equal(date1, betaStats.LastUpdated);
    }

    [Fact]
    public async Task AddAsync_ShouldAddNeighborhood()
    {
        // Arrange
        using var scope = _fixture.Factory!.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<INeighborhoodRepository>();
        var context = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();

        var neighborhood = new Neighborhood { Code = "SINGLE_ADD", Name = "Single Add", City = "SingleCity", Latitude = 1, Longitude = 1, Type = "Buurt" };

        // Act
        var result = await repository.AddAsync(neighborhood);

        // Assert
        Assert.NotNull(result);
        var dbEntity = await context.Neighborhoods.FirstOrDefaultAsync(n => n.Code == "SINGLE_ADD");
        Assert.NotNull(dbEntity);
        Assert.Equal("Single Add", dbEntity.Name);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateNeighborhood()
    {
        // Arrange
        using var scope = _fixture.Factory!.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<INeighborhoodRepository>();
        var context = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();

        var neighborhood = new Neighborhood { Code = "UPDATE_SINGLE", Name = "Before Update", City = "UpdateCity", Latitude = 1, Longitude = 1, Type = "Buurt" };
        await repository.AddAsync(neighborhood);

        context.ChangeTracker.Clear();
        var toUpdate = await context.Neighborhoods.FirstAsync(n => n.Code == "UPDATE_SINGLE");
        toUpdate.Name = "After Update";

        // Act
        await repository.UpdateAsync(toUpdate);

        // Assert
        context.ChangeTracker.Clear();
        var dbEntity = await context.Neighborhoods.FirstAsync(n => n.Code == "UPDATE_SINGLE");
        Assert.Equal("After Update", dbEntity.Name);
        Assert.True(dbEntity.UpdatedAt > DateTime.MinValue);
    }

    [Fact]
    public async Task AddRange_ShouldAddMultipleNeighborhoods()
    {
        // Arrange
        using var scope = _fixture.Factory!.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<INeighborhoodRepository>();
        var context = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();

        var city = "TestAddCity";
        var neighborhoods = new List<Neighborhood>
        {
            new Neighborhood { Code = "TEST_ADD_01", Name = "Test Add 1", City = city, Latitude = 1, Longitude = 1, Type = "Buurt" },
            new Neighborhood { Code = "TEST_ADD_02", Name = "Test Add 2", City = city, Latitude = 2, Longitude = 2, Type = "Buurt" }
        };

        // Act
        repository.AddRange(neighborhoods);
        await repository.SaveChangesAsync();

        // Assert
        var count = await context.Neighborhoods.CountAsync(n => n.City == city);
        Assert.Equal(2, count);

        var saved = await context.Neighborhoods.Where(n => n.City == city).OrderBy(n => n.Code).ToListAsync();
        Assert.Equal("TEST_ADD_01", saved[0].Code);
        Assert.Equal("TEST_ADD_02", saved[1].Code);
    }

    [Fact]
    public async Task UpdateRange_ShouldUpdateMultipleNeighborhoods()
    {
        // Arrange
        using var scope = _fixture.Factory!.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<INeighborhoodRepository>();
        var context = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();

        var city = "TestUpdateCity";
        var neighborhoods = new List<Neighborhood>
        {
            new Neighborhood { Code = "TEST_UPD_03", Name = "Original 3", City = city, Latitude = 3, Longitude = 3, Type = "Buurt" },
            new Neighborhood { Code = "TEST_UPD_04", Name = "Original 4", City = city, Latitude = 4, Longitude = 4, Type = "Buurt" }
        };

        // Initial Seed
        repository.AddRange(neighborhoods);
        await repository.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var toUpdate = await context.Neighborhoods.Where(n => n.City == city).OrderBy(n => n.Code).ToListAsync();
        Assert.Equal(2, toUpdate.Count);

        // Act
        toUpdate[0].Name = "Updated 3";
        toUpdate[1].Name = "Updated 4";

        repository.UpdateRange(toUpdate);
        await repository.SaveChangesAsync();

        // Assert
        context.ChangeTracker.Clear();
        var updated = await context.Neighborhoods.Where(n => n.City == city).OrderBy(n => n.Code).ToListAsync();

        Assert.Equal("Updated 3", updated[0].Name);
        Assert.Equal("Updated 4", updated[1].Name);
        Assert.True(updated[0].UpdatedAt > DateTime.MinValue);
    }
}
