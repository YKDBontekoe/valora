using Valora.Application.Common.Utilities;
using Valora.Application.DTOs.Map;

namespace Valora.Application.Services.Utilities;

/// <summary>
/// Provides high-performance spatial rasterization of vector geometries (GeoJSON) into discrete map tiles.
/// </summary>
public static class OverlayRasterizer
{
    private const int SpatialIndexMultiplier = 5;

    /// <summary>
    /// Converts a collection of vector-based map overlays (polygons) into a grid of uniform raster tiles.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Why Server-Side Rasterization?</strong><br/>
    /// Map overlays (like neighborhoods) are fetched from external APIs (e.g., CBS / PDOK) as large GeoJSON vector shapes.
    /// Rendering dozens of complex polygons with thousands of vertices on the client (Flutter Maps) can cause severe frame drops and lag, especially on older devices.
    /// By rasterizing the data server-side into a grid of simple tiles (similar to a heatmap), we trade visual precision for a massive improvement in client rendering performance.
    /// </para>
    /// <para>
    /// <strong>Spatial Index Optimization:</strong>
    /// A naive rasterization approach checks if the center point of every grid cell falls within every polygon, resulting in O(Cells * Polygons) complexity.
    /// To fix this, we build a temporary spatial index (`Dictionary&lt;(int, int), List&lt;Geometry&gt;&gt;`). The index cell size is a multiple of the requested raster cell size.
    /// Instead of checking every polygon, a given tile only checks the geometries that intersect its larger spatial bucket. This reduces point-in-polygon checks by orders of magnitude.
    /// </para>
    /// </remarks>
    /// <param name="overlays">The list of map overlays containing GeoJSON shapes.</param>
    /// <param name="snappedMinLat">The snapped lower latitude bounds.</param>
    /// <param name="snappedMinLon">The snapped lower longitude bounds.</param>
    /// <param name="snappedMaxLat">The snapped upper latitude bounds.</param>
    /// <param name="snappedMaxLon">The snapped upper longitude bounds.</param>
    /// <param name="cellSize">The size of each raster tile in coordinate degrees.</param>
    /// <returns>A flat list of rasterized tiles, suitable for fast heatmap rendering on the client.</returns>
    public static List<MapOverlayTileDto> RasterizeOverlays(
        IEnumerable<MapOverlayDto> overlays,
        double snappedMinLat,
        double snappedMinLon,
        double snappedMaxLat,
        double snappedMaxLon,
        double cellSize)
    {
        if (cellSize <= 0 || double.IsNaN(cellSize) || double.IsInfinity(cellSize))
        {
            throw new ArgumentException("Cell size must be a positive, finite number.", nameof(cellSize));
        }

        var tiles = new List<MapOverlayTileDto>();

        // Pre-parse geometries for performance
        var parsedOverlays = overlays.Select(overlay =>
            (Dto: overlay, Geometry: GeoUtils.ParseGeometry(overlay.GeoJson))
        ).ToList();

        // Build spatial index
        double indexCellSize = cellSize * SpatialIndexMultiplier;

        if (indexCellSize <= 0 || double.IsNaN(indexCellSize) || double.IsInfinity(indexCellSize))
        {
            throw new ArgumentException("Index cell size must be a positive, finite number.");
        }

        var spatialIndex = new Dictionary<(int, int), List<(MapOverlayDto Dto, GeoUtils.ParsedGeometry Geometry)>>();

        foreach (var parsedOverlay in parsedOverlays)
        {
            int minX = (int)Math.Floor(parsedOverlay.Geometry.BBox.MinLon / indexCellSize);
            int maxX = (int)Math.Floor(parsedOverlay.Geometry.BBox.MaxLon / indexCellSize);
            int minY = (int)Math.Floor(parsedOverlay.Geometry.BBox.MinLat / indexCellSize);
            int maxY = (int)Math.Floor(parsedOverlay.Geometry.BBox.MaxLat / indexCellSize);

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    var key = (x, y);
                    if (!spatialIndex.TryGetValue(key, out var list))
                    {
                        list = new List<(MapOverlayDto Dto, GeoUtils.ParsedGeometry Geometry)>();
                        spatialIndex[key] = list;
                    }
                    list.Add(parsedOverlay);
                }
            }
        }

        // Rasterize: iterate by cellSize, center each tile
        for (double lat = snappedMinLat + cellSize / 2; lat < snappedMaxLat; lat += cellSize)
        {
            int gridY = (int)Math.Floor(lat / indexCellSize);

            for (double lon = snappedMinLon + cellSize / 2; lon < snappedMaxLon; lon += cellSize)
            {
                int gridX = (int)Math.Floor(lon / indexCellSize);
                var key = (gridX, gridY);

                var tile = FindOverlayForPoint(lat, lon, cellSize, key, spatialIndex);
                if (tile != null)
                {
                    tiles.Add(tile);
                }
            }
        }

        return tiles;
    }

    private static MapOverlayTileDto? FindOverlayForPoint(
        double lat,
        double lon,
        double cellSize,
        (int, int) key,
        Dictionary<(int, int), List<(MapOverlayDto Dto, GeoUtils.ParsedGeometry Geometry)>> spatialIndex)
    {
        if (spatialIndex.TryGetValue(key, out var candidates))
        {
            // Simple point-in-polygon check for the center of the tile on candidates only
            var overlayIndex = candidates.FindIndex(o =>
                GeoUtils.IsPointInPolygon(lat, lon, o.Geometry));

            if (overlayIndex >= 0)
            {
                var overlay = candidates[overlayIndex];
                return new MapOverlayTileDto(
                    lat,
                    lon,
                    cellSize,
                    overlay.Dto.MetricValue,
                    overlay.Dto.DisplayValue
                );
            }
        }
        return null;
    }
}
