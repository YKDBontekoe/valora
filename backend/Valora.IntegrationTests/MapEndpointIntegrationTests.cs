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

    [Theory]
    [InlineData(52.0, 4.0, 52.1, 4.1)]
    [InlineData(51.5, 3.5, 52.0, 4.0)]
    public async Task GetMapAmenities_ShouldReturnSuccess_ForValidBbox(double minLat, double minLon, double maxLat, double maxLon)
    {
        // Arrange
        await AuthenticateAsync();
        Factory.AmenityClientMock.Setup(x => x.GetAmenitiesInBboxAsync(
            It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(),
            It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MapAmenityDto>());

        // Act
        var response = await Client.GetAsync($"/api/map/amenities?minLat={minLat}&minLon={minLon}&maxLat={maxLat}&maxLon={maxLon}");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<List<MapAmenityDto>>();
        Assert.NotNull(result);
    }

    [Theory]
    [InlineData(52.0, 4.0, 51.0, 4.1, "minLat must be less than maxLat")]
    [InlineData(52.0, 4.0, 52.1, 3.0, "minLon must be less than maxLon")]
    [InlineData(91.0, 4.0, 92.1, 4.1, "Latitudes must be between -90 and 90")]
    [InlineData(52.0, 4.0, 53.1, 5.1, "Bounding box span too large")]
    public async Task GetMapAmenities_ShouldReturnBadRequest_ForInvalidParams(double minLat, double minLon, double maxLat, double maxLon, string expectedError)
    {
        // Arrange
        await AuthenticateAsync();

        // Act
        var response = await Client.GetAsync($"/api/map/amenities?minLat={minLat}&minLon={minLon}&maxLat={maxLat}&maxLon={maxLon}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains(expectedError, body);
    }

    [Fact]
    public async Task GetMapOverlays_ShouldSupportAllMetrics()
    {
        // Arrange
        await AuthenticateAsync();
        Factory.CbsGeoClientMock.Setup(x => x.GetNeighborhoodOverlaysAsync(
            It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(),
            It.IsAny<MapOverlayMetric>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MapOverlayDto>());

        var metrics = Enum.GetValues<MapOverlayMetric>();
        foreach (var metric in metrics)
        {
            // Act
            var response = await Client.GetAsync($"/api/map/overlays?minLat=52.0&minLon=4.0&maxLat=52.1&maxLon=4.1&metric={metric}");

            // Assert
            response.EnsureSuccessStatusCode();
        }
    }

    [Fact]
    public async Task GetMapAmenityClusters_ShouldReturnSuccess()
    {
        // Arrange
        await AuthenticateAsync();
        Factory.AmenityClientMock.Setup(x => x.GetAmenitiesInBboxAsync(
            It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(),
            It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MapAmenityDto>());

        // Act
        var response = await Client.GetAsync("/api/map/amenities/clusters?minLat=52.0&minLon=4.0&maxLat=52.5&maxLon=4.5&zoom=10");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<List<MapAmenityClusterDto>>();
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetMapOverlayTiles_ShouldReturnSuccess()
    {
        // Arrange
        await AuthenticateAsync();
        Factory.CbsGeoClientMock.Setup(x => x.GetNeighborhoodOverlaysAsync(
            It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(),
            It.IsAny<MapOverlayMetric>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MapOverlayDto>());

        // Act
        var response = await Client.GetAsync("/api/map/overlays/tiles?minLat=52.0&minLon=4.0&maxLat=52.5&maxLon=4.5&zoom=10&metric=PopulationDensity");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<List<MapOverlayTileDto>>();
        Assert.NotNull(result);
    }
}
