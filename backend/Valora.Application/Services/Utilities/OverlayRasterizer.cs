using Valora.Application.Common.Utilities;
using Valora.Application.DTOs.Map;

namespace Valora.Application.Services.Utilities;

public static class OverlayRasterizer
{
    private const int SpatialIndexMultiplier = 5;

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
