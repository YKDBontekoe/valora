using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using System.Net.Http.Json;

namespace Valora.IntegrationTests;

public class MapInsightsIntegrationTests : BaseIntegrationTest
{
    public MapInsightsIntegrationTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task GetCityInsights_ShouldReflectWeightedAverages()
    {
        // Arrange
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
            db.Properties.AddRange(
                new Property { BagId = "I1", Address = "A1", City = "Utrecht", Latitude = 52.09, Longitude = 5.12, ContextCompositeScore = 100 },
                new Property { BagId = "I2", Address = "A2", City = "Utrecht", Latitude = 52.10, Longitude = 5.13, ContextCompositeScore = 50 }
            );
            await db.SaveChangesAsync();
        }

        // Act
        var result = await Client.GetFromJsonAsync<List<Valora.Application.DTOs.Map.MapCityInsightDto>>("/api/map/city-insights");

        // Assert
        var utrecht = result!.First(x => x.City == "Utrecht");
        Assert.Equal(75, utrecht.CompositeScore);
        Assert.Equal(2, utrecht.Count);
    }
}
