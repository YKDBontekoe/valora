using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs.Map;
using Valora.Domain.Entities;
using Xunit;

namespace Valora.IntegrationTests;

public class MapServiceIntegrationTests : BaseIntegrationTest
{
    public MapServiceIntegrationTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task GetMapOverlays_CalculatesPricePerNeighborhood_Correctly()
    {
        // Arrange
        // Seed two listings in different "neighborhoods"
        var listingA = new Listing
        {
            FundaId = "A",
            Address = "Street A",
            Price = 200000,
            LivingAreaM2 = 100, // 2000/m2
            Latitude = 52.0005,
            Longitude = 5.0005,
            ContextReport = null!
        };

        var listingB = new Listing
        {
            FundaId = "B",
            Address = "Street B",
            Price = 500000,
            LivingAreaM2 = 100, // 5000/m2
            Latitude = 52.0025,
            Longitude = 5.0025,
            ContextReport = null!
        };

        DbContext.Listings.AddRange(listingA, listingB);
        await DbContext.SaveChangesAsync();

        // Create mock overlays
        // Overlay A: 52.0000 - 52.0010, 5.0000 - 5.0010 (Contains Listing A)
        var overlayA = new MapOverlayDto(
            "A", "Neighborhood A", "PricePerSquareMeter", 0, "",
            CreatePolygonGeoJson(52.0000, 5.0000, 52.0010, 5.0010));

        // Overlay B: 52.0020 - 52.0030, 5.0020 - 5.0030 (Contains Listing B)
        var overlayB = new MapOverlayDto(
            "B", "Neighborhood B", "PricePerSquareMeter", 0, "",
            CreatePolygonGeoJson(52.0020, 5.0020, 52.0030, 5.0030));

        Factory.CbsGeoClientMock.Setup(x => x.GetNeighborhoodOverlaysAsync(
            It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(),
            MapOverlayMetric.PopulationDensity, It.IsAny<CancellationToken>())) // MapService uses PopulationDensity to get shapes
            .ReturnsAsync(new List<MapOverlayDto> { overlayA, overlayB });

        using var scope = Factory.Services.CreateScope();
        var mapService = scope.ServiceProvider.GetRequiredService<IMapService>();

        // Act
        var result = await mapService.GetMapOverlaysAsync(52.0, 5.0, 52.1, 5.1, MapOverlayMetric.PricePerSquareMeter);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);

        var resultA = result.First(x => x.Id == "A");
        var resultB = result.First(x => x.Id == "B");

        // Use approximate comparison for floating point math
        Assert.Equal(2000, resultA.MetricValue, 0.1);
        Assert.Equal(5000, resultB.MetricValue, 0.1);
    }

    [Fact]
    public async Task GetCityInsights_ReturnsCorrectData()
    {
        // Arrange
        var listing = new Listing
        {
            FundaId = "C",
            Address = "Street C",
            City = "TestCity",
            Latitude = 52.0,
            Longitude = 5.0,
            ContextCompositeScore = 8.5,
            ContextSafetyScore = 9.0,
            ContextSocialScore = 8.0,
            ContextAmenitiesScore = 8.5,
            ContextReport = null!
        };

        DbContext.Listings.Add(listing);
        await DbContext.SaveChangesAsync();

        using var scope = Factory.Services.CreateScope();
        var mapService = scope.ServiceProvider.GetRequiredService<IMapService>();

        // Act
        var result = await mapService.GetCityInsightsAsync();

        // Assert
        Assert.NotNull(result);
        var city = result.FirstOrDefault(c => c.City == "TestCity");
        Assert.NotNull(city);
        Assert.Equal(1, city.Count);
        Assert.Equal(8.5, city.CompositeScore!.Value, 0.1);
    }

    [Fact]
    public async Task GetMapAmenities_RespectsBoundingBox()
    {
        // Arrange
        Factory.AmenityClientMock.Setup(x => x.GetAmenitiesInBboxAsync(
             It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(),
             It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(new List<MapAmenityDto>());

        using var scope = Factory.Services.CreateScope();
        var mapService = scope.ServiceProvider.GetRequiredService<IMapService>();

        // Act
        var result = await mapService.GetMapAmenitiesAsync(52.0, 5.0, 52.1, 5.1);

        // Assert
        Assert.NotNull(result);
        Factory.AmenityClientMock.Verify(x => x.GetAmenitiesInBboxAsync(
            52.0, 5.0, 52.1, 5.1, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static JsonElement CreatePolygonGeoJson(double minLat, double minLon, double maxLat, double maxLon)
    {
        // Coordinates in GeoJSON are [lon, lat]
        var polygon = new
        {
            type = "Polygon",
            coordinates = new[] {
                new[] {
                    new[] { minLon, minLat },
                    new[] { maxLon, minLat },
                    new[] { maxLon, maxLat },
                    new[] { minLon, maxLat },
                    new[] { minLon, minLat }
                }
            }
        };
        return JsonSerializer.SerializeToElement(polygon);
    }
}
