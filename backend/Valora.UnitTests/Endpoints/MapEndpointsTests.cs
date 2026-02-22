using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using Valora.Api.Endpoints;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs.Map;
using Valora.Application.DTOs.Shared;
using Xunit;

namespace Valora.UnitTests.Endpoints;

public class MapEndpointsTests
{
    private readonly Mock<IMapService> _mapServiceMock;

    public MapEndpointsTests()
    {
        _mapServiceMock = new Mock<IMapService>();
    }

    [Fact]
    public async Task GetCityInsightsHandler_ReturnsOk()
    {
        // Arrange
        var insights = new List<MapCityInsightDto> { new MapCityInsightDto("City", 1, 0, 0, null, null, null, null) };
        _mapServiceMock.Setup(x => x.GetCityInsightsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(insights);

        // Act
        var result = await MapEndpoints.GetCityInsightsHandler(_mapServiceMock.Object, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<Ok<List<MapCityInsightDto>>>(result);
        Assert.Single(okResult.Value!);
    }

    [Fact]
    public async Task GetMapAmenitiesHandler_ParsesTypesAndReturnsOk()
    {
        // Arrange
        List<string>? capturedTypes = null;
        var amenities = new List<MapAmenityDto> { new MapAmenityDto("id", "type", "name", 0, 0, null) };
        var bounds = new BoundsRequest(52, 4, 53, 5);
        
        _mapServiceMock.Setup(x => x.GetMapAmenitiesAsync(
            It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(),
            It.IsAny<List<string>?>(), It.IsAny<CancellationToken>()))
            .Callback<double, double, double, double, List<string>?, CancellationToken>((_, _, _, _, types, _) => capturedTypes = types)
            .ReturnsAsync(amenities);

        // Act
        var result = await MapEndpoints.GetMapAmenitiesHandler(bounds, " school , park ", _mapServiceMock.Object, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<Ok<List<MapAmenityDto>>>(result);
        Assert.Single(okResult.Value!);
        Assert.NotNull(capturedTypes);
        Assert.Equal(2, capturedTypes.Count);
        Assert.Contains("school", capturedTypes);
        Assert.Contains("park", capturedTypes);
    }

    [Fact]
    public async Task GetMapAmenitiesHandler_HandlesNullTypes()
    {
        var bounds = new BoundsRequest(52, 4, 53, 5);
        _mapServiceMock.Setup(x => x.GetMapAmenitiesAsync(
            It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(),
            It.IsAny<List<string>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MapAmenityDto>());

        var result = await MapEndpoints.GetMapAmenitiesHandler(bounds, null, _mapServiceMock.Object, CancellationToken.None);

        Assert.IsType<Ok<List<MapAmenityDto>>>(result);
    }

    [Fact]
    public async Task GetMapAmenityClustersHandler_ReturnsOk()
    {
        var bounds = new BoundsRequest(52, 4, 53, 5);
        _mapServiceMock.Setup(x => x.GetMapAmenityClustersAsync(
            It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(),
            It.IsAny<List<string>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MapAmenityClusterDto>());

        var result = await MapEndpoints.GetMapAmenityClustersHandler(bounds, 10, null, _mapServiceMock.Object, CancellationToken.None);

        Assert.IsType<Ok<List<MapAmenityClusterDto>>>(result);
    }

    [Fact]
    public async Task GetMapOverlaysHandler_ReturnsOk()
    {
        // Arrange
        var bounds = new BoundsRequest(52, 4, 53, 5);
        var overlays = new List<MapOverlayDto> { new MapOverlayDto("id", "name", "metric", 0, "val", default) };
        _mapServiceMock.Setup(x => x.GetMapOverlaysAsync(
            It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(),
            It.IsAny<MapOverlayMetric>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(overlays);

        // Act
        var result = await MapEndpoints.GetMapOverlaysHandler(bounds, MapOverlayMetric.PopulationDensity, _mapServiceMock.Object, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<Ok<List<MapOverlayDto>>>(result);
        Assert.Single(okResult.Value!);
    }

    [Fact]
    public async Task GetMapOverlayTilesHandler_ReturnsOk()
    {
        var bounds = new BoundsRequest(52, 4, 53, 5);
        _mapServiceMock.Setup(x => x.GetMapOverlayTilesAsync(
            It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(),
            It.IsAny<MapOverlayMetric>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MapOverlayTileDto>());

        var result = await MapEndpoints.GetMapOverlayTilesHandler(bounds, 10, MapOverlayMetric.PopulationDensity, _mapServiceMock.Object, CancellationToken.None);

        Assert.IsType<Ok<List<MapOverlayTileDto>>>(result);
    }
}
