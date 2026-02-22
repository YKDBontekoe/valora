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
        await AuthenticateAsync();
        var response = await Client.GetAsync("/api/map/cities");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<List<MapCityInsightDto>>();
        Assert.NotNull(result);
    }

    [Theory]
    [InlineData(52.0, 4.0, 52.1, 4.1)]
    public async Task GetMapAmenities_ShouldReturnSuccess_ForValidBbox(double minLat, double minLon, double maxLat, double maxLon)
    {
        await AuthenticateAsync();
        Factory.AmenityClientMock.Setup(x => x.GetAmenitiesInBboxAsync(
            It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(),
            It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MapAmenityDto>());

        var response = await Client.GetAsync($"/api/map/amenities?minLat={minLat}&minLon={minLon}&maxLat={maxLat}&maxLon={maxLon}");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<List<MapAmenityDto>>();
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetMapAmenities_ShouldParseTypes()
    {
        await AuthenticateAsync();
        List<string> capturedTypes = null;
        Factory.AmenityClientMock.Setup(x => x.GetAmenitiesInBboxAsync(
            It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(),
            It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .Callback<double, double, double, double, List<string>, CancellationToken>((_, _, _, _, types, _) => capturedTypes = types)
            .ReturnsAsync(new List<MapAmenityDto>());

        var response = await Client.GetAsync($"/api/map/amenities?minLat=52&minLon=4&maxLat=52.1&maxLon=4.1&types=school,park");

        response.EnsureSuccessStatusCode();
        Assert.NotNull(capturedTypes);
        Assert.Contains("school", capturedTypes);
        Assert.Contains("park", capturedTypes);
    }

    [Theory]
    [InlineData(52.0, 4.0, 51.0, 4.1, "minLat must be less than maxLat")]
    public async Task GetMapAmenities_ShouldReturnBadRequest_ForInvalidParams(double minLat, double minLon, double maxLat, double maxLon, string expectedError)
    {
        await AuthenticateAsync();
        var response = await Client.GetAsync($"/api/map/amenities?minLat={minLat}&minLon={minLon}&maxLat={maxLat}&maxLon={maxLon}");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetMapOverlays_ShouldSupportAllMetrics()
    {
        await AuthenticateAsync();
        Factory.CbsGeoClientMock.Setup(x => x.GetNeighborhoodOverlaysAsync(
            It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(),
            It.IsAny<MapOverlayMetric>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MapOverlayDto>());

        var metrics = Enum.GetValues<MapOverlayMetric>();
        foreach (var metric in metrics)
        {
            var response = await Client.GetAsync($"/api/map/overlays?minLat=52.0&minLon=4.0&maxLat=52.1&maxLon=4.1&metric={metric}");
            response.EnsureSuccessStatusCode();
        }
    }

    [Fact]
    public async Task GetMapAmenityClusters_ShouldReturnSuccess()
    {
        await AuthenticateAsync();
        Factory.AmenityClientMock.Setup(x => x.GetAmenitiesInBboxAsync(
            It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(),
            It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MapAmenityDto>());

        var response = await Client.GetAsync("/api/map/amenities/clusters?minLat=52.0&minLon=4.0&maxLat=52.5&maxLon=4.5&zoom=10");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<List<MapAmenityClusterDto>>();
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetMapAmenityClusters_ShouldParseTypes()
    {
        await AuthenticateAsync();
        List<string> capturedTypes = null;
        Factory.AmenityClientMock.Setup(x => x.GetAmenitiesInBboxAsync(
            It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(),
            It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .Callback<double, double, double, double, List<string>, CancellationToken>((_, _, _, _, types, _) => capturedTypes = types)
            .ReturnsAsync(new List<MapAmenityDto>());

        var response = await Client.GetAsync("/api/map/amenities/clusters?minLat=52.0&minLon=4.0&maxLat=52.5&maxLon=4.5&zoom=10&types=school,park");

        response.EnsureSuccessStatusCode();
        Assert.NotNull(capturedTypes);
        Assert.Contains("school", capturedTypes);
    }

    [Fact]
    public async Task GetMapOverlayTiles_ShouldReturnSuccess()
    {
        await AuthenticateAsync();
        Factory.CbsGeoClientMock.Setup(x => x.GetNeighborhoodOverlaysAsync(
            It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(),
            It.IsAny<MapOverlayMetric>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MapOverlayDto>());

        var response = await Client.GetAsync("/api/map/overlays/tiles?minLat=52.0&minLon=4.0&maxLat=52.5&maxLon=4.5&zoom=10&metric=PopulationDensity");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<List<MapOverlayTileDto>>();
        Assert.NotNull(result);
    }
}
