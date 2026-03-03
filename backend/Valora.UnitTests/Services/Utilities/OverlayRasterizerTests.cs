using System.Text.Json;
using Valora.Application.DTOs.Map;
using Valora.Application.Services.Utilities;
using Xunit;

namespace Valora.UnitTests.Services.Utilities;

public class OverlayRasterizerTests
{
    [Fact]
    public void RasterizeOverlays_GeneratesTilesCorrectly()
    {
        // Arrange
        var jsonStr = "{\"type\":\"Polygon\",\"coordinates\":[[[4.0,52.0],[4.2,52.0],[4.2,52.2],[4.0,52.2],[4.0,52.0]]]}";
        using var jsonDoc = JsonDocument.Parse(jsonStr);

        var overlays = new List<MapOverlayDto>
        {
            new("BU00000000", "Neighborhood 1", "Metric1", 10.0, "10", jsonDoc.RootElement.Clone()) // BBox: 4.0-4.2, 52.0-52.2
        };

        var snappedMinLat = 52.0;
        var snappedMinLon = 4.0;
        var snappedMaxLat = 52.2;
        var snappedMaxLon = 4.2;
        var cellSize = 0.1;

        // Act
        var tiles = OverlayRasterizer.RasterizeOverlays(
            overlays,
            snappedMinLat,
            snappedMinLon,
            snappedMaxLat,
            snappedMaxLon,
            cellSize);

        // Assert
        // Area is 0.2 x 0.2, cellSize is 0.1 => 4 cells total
        Assert.Equal(4, tiles.Count);

        foreach (var tile in tiles)
        {
            Assert.Equal(0.1, tile.Size);
            Assert.Equal(10.0, tile.Value);
            Assert.Equal("10", tile.DisplayValue);

            // Verify centers are correct with small tolerance for floating point math
            Assert.True(Math.Abs(tile.Latitude - 52.05) < 0.001 || Math.Abs(tile.Latitude - 52.15) < 0.001);
            Assert.True(Math.Abs(tile.Longitude - 4.05) < 0.001 || Math.Abs(tile.Longitude - 4.15) < 0.001);
        }
    }

    [Fact]
    public void RasterizeOverlays_ReturnsEmpty_WhenNoOverlays()
    {
        // Arrange
        var overlays = new List<MapOverlayDto>();

        var snappedMinLat = 52.0;
        var snappedMinLon = 4.0;
        var snappedMaxLat = 52.2;
        var snappedMaxLon = 4.2;
        var cellSize = 0.1;

        // Act
        var tiles = OverlayRasterizer.RasterizeOverlays(
            overlays,
            snappedMinLat,
            snappedMinLon,
            snappedMaxLat,
            snappedMaxLon,
            cellSize);

        // Assert
        Assert.Empty(tiles);
    }
}
