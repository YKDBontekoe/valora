using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using Valora.Api.Endpoints;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs.Map;
using Xunit;

namespace Valora.UnitTests.Endpoints;

public class MapEndpointsTests
{
    private readonly Mock<IMapService> _mockMapService = new();

    [Fact]
    public async Task GetCityInsights_ReturnsOk()
    {
        // Arrange
        var insights = new List<MapCityInsightDto> { new MapCityInsightDto("City", 1, 0, 0, null, null, null, null) };
        _mockMapService.Setup(x => x.GetCityInsightsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(insights);

        // Act
        var handler = async (IMapService service, CancellationToken ct) =>
        {
            var result = await service.GetCityInsightsAsync(ct);
            return Results.Ok(result);
        };
        var result = await handler(_mockMapService.Object, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<Ok<List<MapCityInsightDto>>>(result);
        Assert.Single(okResult.Value!);
    }

    [Fact]
    public async Task GetMapAmenities_ReturnsOk()
    {
        // Arrange
        var amenities = new List<MapAmenityDto> { new MapAmenityDto("id", "type", "name", 0, 0, null) };
        _mockMapService.Setup(x => x.GetMapAmenitiesAsync(
            It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(),
            It.IsAny<List<string>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(amenities);

        // Act
        var handler = async (double minLat, double minLon, double maxLat, double maxLon, string? types, IMapService service, CancellationToken ct) =>
        {
            var typeList = types?.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .ToList();
            var result = await service.GetMapAmenitiesAsync(minLat, minLon, maxLat, maxLon, typeList, ct);
            return Results.Ok(result);
        };
        var result = await handler(52.0, 4.0, 52.1, 4.1, "park,cafe", _mockMapService.Object, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<Ok<List<MapAmenityDto>>>(result);
        Assert.Single(okResult.Value!);
    }

    [Fact]
    public async Task GetMapOverlays_ReturnsOk()
    {
        // Arrange
        var overlays = new List<MapOverlayDto> { new MapOverlayDto("id", "name", "metric", 0, "val", default) };
        _mockMapService.Setup(x => x.GetMapOverlaysAsync(
            It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(),
            It.IsAny<MapOverlayMetric>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(overlays);

        // Act
        var handler = async (double minLat, double minLon, double maxLat, double maxLon, MapOverlayMetric metric, IMapService service, CancellationToken ct) =>
        {
            var result = await service.GetMapOverlaysAsync(minLat, minLon, maxLat, maxLon, metric, ct);
            return Results.Ok(result);
        };
        var result = await handler(52.0, 4.0, 52.1, 4.1, MapOverlayMetric.PopulationDensity, _mockMapService.Object, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<Ok<List<MapOverlayDto>>>(result);
        Assert.Single(okResult.Value!);
    }
}
