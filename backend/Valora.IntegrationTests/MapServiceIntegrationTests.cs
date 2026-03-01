using Valora.Domain.Entities;
using Valora.Application.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Valora.Infrastructure.Persistence;
using Xunit;
using System.Net.Http.Json;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

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
            var cache = scope.ServiceProvider.GetRequiredService<IMemoryCache>();
            if (cache is MemoryCache memoryCache)
            {
                memoryCache.Compact(1.0); // Clear cache before test to avoid state bleeding from other tests
            }
            db.Properties.Add(new Property { BagId = "M1", Address = "A1", City = "Amsterdam", Latitude = 52.3, Longitude = 4.9, ContextCompositeScore = 80 });
            await db.SaveChangesAsync();
        }

        // Act
        var result = await Client.GetFromJsonAsync<List<Valora.Application.DTOs.Map.MapCityInsightDto>>("/api/map/city-insights");

        // Assert
        Assert.NotNull(result);
        var nonNullResult = result!;
        Assert.Contains(nonNullResult, x => x.City == "Amsterdam");
    }
}
