using System.Net;
using System.Net.Http.Json;
using Moq;
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
        Factory.AmenityClientMock.Setup(x => x.GetAmenitiesInBboxAsync(
            It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(),
            It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MapAmenityDto>());

        // Act
        var response = await Client.GetAsync("/api/map/amenities?minLat=52.0&minLon=4.0&maxLat=52.1&maxLon=4.1");

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
        Factory.CbsGeoClientMock.Setup(x => x.GetNeighborhoodOverlaysAsync(
            It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(),
            It.IsAny<MapOverlayMetric>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MapOverlayDto>());

        // Act
        var response = await Client.GetAsync("/api/map/overlays?minLat=52.0&minLon=4.0&maxLat=52.1&maxLon=4.1&metric=PopulationDensity");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<List<MapOverlayDto>>();
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetMapAmenities_WithInvalidBbox_ShouldReturnBadRequest()
    {
        // Arrange
        await AuthenticateAsync();

        // Act
        var response = await Client.GetAsync("/api/map/amenities?minLat=60&minLon=4&maxLat=50&maxLon=5");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
