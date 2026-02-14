using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Moq;
using Valora.Application.DTOs.Map;
using Valora.Domain.Entities;

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
    [InlineData(51.5, 3.5, 52.0, 4.0)]
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

    [Theory]
    [InlineData(52.0, 4.0, 51.0, 4.1, "minLat must be less than maxLat")]
    [InlineData(52.0, 4.0, 52.1, 3.0, "minLon must be less than maxLon")]
    [InlineData(91.0, 4.0, 92.1, 4.1, "Latitudes must be between -90 and 90")]
    [InlineData(52.0, 4.0, 53.1, 5.1, "Bounding box span too large")]
    public async Task GetMapAmenities_ShouldReturnBadRequest_ForInvalidParams(double minLat, double minLon, double maxLat, double maxLon, string expectedError)
    {
        await AuthenticateAsync();

        var response = await Client.GetAsync($"/api/map/amenities?minLat={minLat}&minLon={minLon}&maxLat={maxLat}&maxLon={maxLon}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains(expectedError, body);
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
    public async Task GetMapOverlays_PricePerSquareMeter_ShouldReturnDistinctNeighborhoodMetrics()
    {
        await AuthenticateAsync();

        DbContext.Listings.AddRange(
            new Listing { FundaId = "n1-1", Address = "N1 A", Latitude = 52.010, Longitude = 5.010, Price = 500000, LivingAreaM2 = 100 },
            new Listing { FundaId = "n1-2", Address = "N1 B", Latitude = 52.020, Longitude = 5.020, Price = 550000, LivingAreaM2 = 100 },
            new Listing { FundaId = "n1-3", Address = "N1 C", Latitude = 52.030, Longitude = 5.030, Price = 600000, LivingAreaM2 = 100 },
            new Listing { FundaId = "n2-1", Address = "N2 A", Latitude = 52.080, Longitude = 5.080, Price = 200000, LivingAreaM2 = 100 },
            new Listing { FundaId = "n2-2", Address = "N2 B", Latitude = 52.085, Longitude = 5.085, Price = 210000, LivingAreaM2 = 100 },
            new Listing { FundaId = "n2-3", Address = "N2 C", Latitude = 52.090, Longitude = 5.090, Price = 220000, LivingAreaM2 = 100 });
        await DbContext.SaveChangesAsync();

        Factory.CbsGeoClientMock.Setup(x => x.GetNeighborhoodOverlaysAsync(
                It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(),
                MapOverlayMetric.PopulationDensity, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MapOverlayDto>
            {
                new("BU001", "North", "PopulationDensity", 100, "100", CreatePolygonFeature(5.00, 52.00, 5.05, 52.05)),
                new("BU002", "South", "PopulationDensity", 120, "120", CreatePolygonFeature(5.07, 52.07, 5.10, 52.10))
            });

        var response = await Client.GetAsync("/api/map/overlays?minLat=52.0&minLon=5.0&maxLat=52.2&maxLon=5.2&metric=PricePerSquareMeter");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<List<MapOverlayDto>>();

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);

        var north = Assert.Single(result.Where(x => x.Id == "BU001"));
        var south = Assert.Single(result.Where(x => x.Id == "BU002"));

        Assert.Equal(5500, north.MetricValue);
        Assert.Equal(2100, south.MetricValue);
        Assert.NotEqual(north.MetricValue, south.MetricValue);
        Assert.True(north.HasSufficientData);
        Assert.True(south.HasSufficientData);
    }

    private static JsonElement CreatePolygonFeature(double minLon, double minLat, double maxLon, double maxLat)
    {
        var payload = $$"""
            {
                "type": "Feature",
                "geometry": {
                    "type": "Polygon",
                    "coordinates": [[
                        [{{minLon}}, {{minLat}}],
                        [{{maxLon}}, {{minLat}}],
                        [{{maxLon}}, {{maxLat}}],
                        [{{minLon}}, {{maxLat}}],
                        [{{minLon}}, {{minLat}}]
                    ]]
                }
            }
            """;

        return JsonDocument.Parse(payload).RootElement.Clone();
    }
}
