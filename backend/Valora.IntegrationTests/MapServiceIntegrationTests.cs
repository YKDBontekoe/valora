using Valora.Domain.Entities;
using Valora.Application.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Valora.Infrastructure.Persistence;
using Xunit;
using System.Net.Http.Json;

namespace Valora.IntegrationTests;

public class MapServiceIntegrationTests : BaseIntegrationTest
{
    public MapServiceIntegrationTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task GetCityInsights_ShouldReturnSeededData()
    {
        // Arrange
        await AuthenticateAsync();
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
            db.Properties.Add(new Property { BagId = "M1", Address = "A1", City = "Amsterdam", Latitude = 52.3, Longitude = 4.9, ContextCompositeScore = 80 });
            await db.SaveChangesAsync();
        }

        // Act
        var result = await Client.GetFromJsonAsync<List<Valora.Application.DTOs.Map.MapCityInsightDto>>("/api/map/city-insights");

        // Assert
        Assert.NotNull(result);
        var nonNullResult = result!;
        Assert.NotEmpty(nonNullResult);
        Assert.Contains(nonNullResult, x => x.City == "Amsterdam");
    }
}
