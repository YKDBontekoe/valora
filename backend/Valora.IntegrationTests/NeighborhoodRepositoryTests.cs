using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Valora.Application.Common.Interfaces;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;
using Xunit;

namespace Valora.IntegrationTests;

[Collection("TestcontainersDatabase")]
public class NeighborhoodRepositoryTests
{
    private readonly TestcontainersDatabaseFixture _fixture;

    public NeighborhoodRepositoryTests(TestcontainersDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AddRange_ShouldAddMultipleNeighborhoods()
    {
        // Arrange
        using var scope = _fixture.Factory.Services.CreateScope();
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
        using var scope = _fixture.Factory.Services.CreateScope();
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

        // Detach to simulate fetching in a new context or untracked update scenario if needed,
        // but here we can just update the tracked entities or fetch new ones.
        // Let's fetch them fresh to be sure.
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
