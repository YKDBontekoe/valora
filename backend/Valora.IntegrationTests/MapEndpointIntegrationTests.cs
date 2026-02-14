using System.Net;
using System.Net.Http.Json;
using Valora.Application.DTOs.Map;

namespace Valora.IntegrationTests;

public class MapEndpointIntegrationTests : BaseIntegrationTest
{
    public MapEndpointIntegrationTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task GetCityInsights_ShouldReturnSuccess()
    {
        // Arrange
        await AuthenticateAsync();

        // Act
        var response = await Client.GetAsync("/api/map/cities");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<List<MapCityInsightDto>>();
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetMapAmenities_ShouldReturnSuccess()
    {
        // Arrange
        await AuthenticateAsync();

        // Act
        var response = await Client.GetAsync("/api/map/amenities?minLat=52.0&minLon=4.0&maxLat=52.5&maxLon=5.0");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<List<MapAmenityDto>>();
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetMapOverlays_ShouldReturnSuccess()
    {
        // Arrange
        await AuthenticateAsync();

        // Act
        var response = await Client.GetAsync("/api/map/overlays?minLat=52.0&minLon=4.0&maxLat=52.1&maxLon=4.1&metric=PopulationDensity");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<List<MapOverlayDto>>();
        Assert.NotNull(result);
    }
}
