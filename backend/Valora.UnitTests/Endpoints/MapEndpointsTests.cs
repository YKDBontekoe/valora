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
    private readonly Mock<IMapService> _mapServiceMock;

    public MapEndpointsTests()
    {
        _mapServiceMock = new Mock<IMapService>();
    }

    [Fact]
    public async Task GetCityInsightsHandler_ReturnsOk()
    {
        _mapServiceMock.Setup(x => x.GetCityInsightsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MapCityInsightDto>());

        var result = await MapEndpoints.GetCityInsightsHandler(_mapServiceMock.Object, CancellationToken.None);

        Assert.IsType<Ok<List<MapCityInsightDto>>>(result);
    }

    [Fact]
    public async Task GetMapAmenitiesHandler_ParsesTypesAndReturnsOk()
    {
        List<string> capturedTypes = null;
        _mapServiceMock.Setup(x => x.GetMapAmenitiesAsync(
            It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(),
            It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .Callback<double, double, double, double, List<string>, CancellationToken>((_, _, _, _, types, _) => capturedTypes = types)
            .ReturnsAsync(new List<MapAmenityDto>());

        var result = await MapEndpoints.GetMapAmenitiesHandler(52, 4, 53, 5, " school , park ", _mapServiceMock.Object, CancellationToken.None);

        Assert.IsType<Ok<List<MapAmenityDto>>>(result);
        Assert.NotNull(capturedTypes);
        Assert.Equal(2, capturedTypes.Count);
        Assert.Contains("school", capturedTypes);
        Assert.Contains("park", capturedTypes);
    }

    [Fact]
    public async Task GetMapAmenityClustersHandler_ReturnsOk()
    {
        _mapServiceMock.Setup(x => x.GetMapAmenityClustersAsync(
            It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(),
            It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MapAmenityClusterDto>());

        var result = await MapEndpoints.GetMapAmenityClustersHandler(52, 4, 53, 5, 10, null, _mapServiceMock.Object, CancellationToken.None);

        Assert.IsType<Ok<List<MapAmenityClusterDto>>>(result);
    }

    [Fact]
    public async Task GetMapOverlaysHandler_ReturnsOk()
    {
        _mapServiceMock.Setup(x => x.GetMapOverlaysAsync(
            It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(),
            It.IsAny<MapOverlayMetric>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MapOverlayDto>());

        var result = await MapEndpoints.GetMapOverlaysHandler(52, 4, 53, 5, MapOverlayMetric.PopulationDensity, _mapServiceMock.Object, CancellationToken.None);

        Assert.IsType<Ok<List<MapOverlayDto>>>(result);
    }

    [Fact]
    public async Task GetMapOverlayTilesHandler_ReturnsOk()
    {
        _mapServiceMock.Setup(x => x.GetMapOverlayTilesAsync(
            It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(),
            It.IsAny<MapOverlayMetric>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MapOverlayTileDto>());

        var result = await MapEndpoints.GetMapOverlayTilesHandler(52, 4, 53, 5, 10, MapOverlayMetric.PopulationDensity, _mapServiceMock.Object, CancellationToken.None);

        Assert.IsType<Ok<List<MapOverlayTileDto>>>(result);
    }
}
