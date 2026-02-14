using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Moq;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs.Map;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;
using Valora.Infrastructure.Services;

namespace Valora.UnitTests.Services;

public class MapServiceTests : IDisposable
{
    private readonly ValoraDbContext _context;
    private readonly Mock<IAmenityClient> _amenityClientMock;
    private readonly Mock<ICbsGeoClient> _cbsGeoClientMock;
    private readonly IMapService _mapService;

    public MapServiceTests()
    {
        var options = new DbContextOptionsBuilder<ValoraDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ValoraDbContext(options);
        _amenityClientMock = new Mock<IAmenityClient>();
        _cbsGeoClientMock = new Mock<ICbsGeoClient>();

        _mapService = new MapService(_context, _amenityClientMock.Object, _cbsGeoClientMock.Object);
    }

    [Fact]
    public async Task GetCityInsightsAsync_ShouldReturnGroupedInsights()
    {
        var listings = new List<Listing>
        {
            new Listing { FundaId = "1", Address = "A1", City = "Utrecht", Latitude = 52.0, Longitude = 5.0, ContextCompositeScore = 80 },
            new Listing { FundaId = "2", Address = "A2", City = "Utrecht", Latitude = 52.1, Longitude = 5.1, ContextCompositeScore = 60 },
            new Listing { FundaId = "3", Address = "A3", City = "Amsterdam", Latitude = 52.3, Longitude = 4.9, ContextCompositeScore = 90 }
        };

        _context.Listings.AddRange(listings);
        await _context.SaveChangesAsync();

        var result = await _mapService.GetCityInsightsAsync();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, x => x.City == "Utrecht" && x.Count == 2);
        Assert.Contains(result, x => x.City == "Amsterdam" && x.Count == 1);
    }

    [Fact]
    public async Task GetMapAmenitiesAsync_ShouldCallClient()
    {
        var amenities = new List<MapAmenityDto> { new MapAmenityDto("1", "school", "Test School", 52.0, 5.0) };
        _amenityClientMock.Setup(x => x.GetAmenitiesInBboxAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(amenities);

        var result = await _mapService.GetMapAmenitiesAsync(52.0, 5.0, 52.1, 5.1);

        Assert.Single(result);
        Assert.Equal("Test School", result[0].Name);
    }

    [Fact]
    public async Task GetMapOverlaysAsync_PricePerSquareMeter_ShouldUseNeighborhoodCodeFromFeatures()
    {
        _context.Listings.AddRange(
            new Listing
            {
                FundaId = "1",
                Address = "A1",
                Latitude = 52.05,
                Longitude = 5.05,
                Price = 500000,
                LivingAreaM2 = 100,
                Features = new Dictionary<string, string> { ["buurtcode"] = "BU001" }
            },
            new Listing
            {
                FundaId = "2",
                Address = "A2",
                Latitude = 52.055,
                Longitude = 5.055,
                Price = 300000,
                LivingAreaM2 = 100,
                Features = new Dictionary<string, string> { ["buurtcode"] = "BU001" }
            },
            new Listing
            {
                FundaId = "3",
                Address = "A3",
                Latitude = 52.08,
                Longitude = 5.08,
                Price = 200000,
                LivingAreaM2 = 100,
                Features = new Dictionary<string, string> { ["buurtcode"] = "BU002" }
            },
            new Listing
            {
                FundaId = "4",
                Address = "A4",
                Latitude = 52.085,
                Longitude = 5.085,
                Price = 220000,
                LivingAreaM2 = 100,
                Features = new Dictionary<string, string> { ["buurtcode"] = "BU002" }
            },
            new Listing
            {
                FundaId = "5",
                Address = "A5",
                Latitude = 52.086,
                Longitude = 5.086,
                Price = 240000,
                LivingAreaM2 = 100,
                Features = new Dictionary<string, string> { ["buurtcode"] = "BU002" }
            });
        await _context.SaveChangesAsync();

        var overlays = new List<MapOverlayDto>
        {
            new("BU001", "North", "PopulationDensity", 100, "100", CreatePolygonFeature(5.00, 52.00, 5.07, 52.07)),
            new("BU002", "South", "PopulationDensity", 110, "110", CreatePolygonFeature(5.07, 52.07, 5.10, 52.10))
        };

        _cbsGeoClientMock.Setup(x => x.GetNeighborhoodOverlaysAsync(
                It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(),
                MapOverlayMetric.PopulationDensity, It.IsAny<CancellationToken>()))
            .ReturnsAsync(overlays);

        var result = await _mapService.GetMapOverlaysAsync(52.0, 5.0, 52.2, 5.2, MapOverlayMetric.PricePerSquareMeter);

        Assert.Equal(2, result.Count);

        var bu001 = Assert.Single(result.Where(x => x.Id == "BU001"));
        Assert.Equal(4000, bu001.MetricValue);
        Assert.Equal(4000, bu001.SecondaryMetricValue);
        Assert.Equal(2, bu001.SampleSize);
        Assert.False(bu001.HasSufficientData);
        Assert.Contains("Low confidence", bu001.DisplayValue);

        var bu002 = Assert.Single(result.Where(x => x.Id == "BU002"));
        Assert.Equal(2200, bu002.MetricValue);
        Assert.Equal(2200, bu002.SecondaryMetricValue);
        Assert.Equal(3, bu002.SampleSize);
        Assert.True(bu002.HasSufficientData);
        Assert.Contains("Median", bu002.DisplayValue);
    }

    [Fact]
    public async Task GetMapOverlaysAsync_PricePerSquareMeter_ShouldFallbackToPolygonLookup()
    {
        _context.Listings.AddRange(
            new Listing { FundaId = "1", Address = "A1", Latitude = 52.010, Longitude = 5.010, Price = 500000, LivingAreaM2 = 100 },
            new Listing { FundaId = "2", Address = "A2", Latitude = 52.020, Longitude = 5.020, Price = 550000, LivingAreaM2 = 100 },
            new Listing { FundaId = "3", Address = "A3", Latitude = 52.030, Longitude = 5.030, Price = 600000, LivingAreaM2 = 100 },
            new Listing { FundaId = "4", Address = "A4", Latitude = 52.080, Longitude = 5.080, Price = 200000, LivingAreaM2 = 100 },
            new Listing { FundaId = "5", Address = "A5", Latitude = 52.085, Longitude = 5.085, Price = 210000, LivingAreaM2 = 100 },
            new Listing { FundaId = "6", Address = "A6", Latitude = 52.090, Longitude = 5.090, Price = 220000, LivingAreaM2 = 100 });
        await _context.SaveChangesAsync();

        var overlays = new List<MapOverlayDto>
        {
            new("BU001", "North", "PopulationDensity", 100, "100", CreatePolygonFeature(5.00, 52.00, 5.05, 52.05)),
            new("BU002", "South", "PopulationDensity", 110, "110", CreatePolygonFeature(5.07, 52.07, 5.10, 52.10))
        };

        _cbsGeoClientMock.Setup(x => x.GetNeighborhoodOverlaysAsync(
                It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(),
                MapOverlayMetric.PopulationDensity, It.IsAny<CancellationToken>()))
            .ReturnsAsync(overlays);

        var result = await _mapService.GetMapOverlaysAsync(52.0, 5.0, 52.2, 5.2, MapOverlayMetric.PricePerSquareMeter);

        var bu001 = Assert.Single(result.Where(x => x.Id == "BU001"));
        var bu002 = Assert.Single(result.Where(x => x.Id == "BU002"));

        Assert.Equal(5500, bu001.MetricValue);
        Assert.Equal(2100, bu002.MetricValue);
        Assert.NotEqual(bu001.MetricValue, bu002.MetricValue);
        Assert.True(bu001.HasSufficientData);
        Assert.True(bu002.HasSufficientData);
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

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
