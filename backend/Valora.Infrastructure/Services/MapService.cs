using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs.Map;
using Valora.Infrastructure.Persistence;

namespace Valora.Infrastructure.Services;

public class MapService : IMapService
{
    private const int MinimumSampleSize = 3;

    private readonly ValoraDbContext _context;
    private readonly IAmenityClient _amenityClient;
    private readonly ICbsGeoClient _cbsGeoClient;

    public MapService(
        ValoraDbContext context,
        IAmenityClient amenityClient,
        ICbsGeoClient cbsGeoClient)
    {
        _context = context;
        _amenityClient = amenityClient;
        _cbsGeoClient = cbsGeoClient;
    }

    public async Task<List<MapCityInsightDto>> GetCityInsightsAsync(CancellationToken cancellationToken = default)
    {
        var query = _context.Listings
            .Where(x => x.City != null && x.Latitude.HasValue && x.Longitude.HasValue)
            .GroupBy(x => x.City!)
            .Select(g => new MapCityInsightDto(
                g.Key,
                g.Count(),
                g.Average(x => x.Latitude!.Value),
                g.Average(x => x.Longitude!.Value),
                g.Average(x => x.ContextCompositeScore),
                g.Average(x => x.ContextSafetyScore),
                g.Average(x => x.ContextSocialScore),
                g.Average(x => x.ContextAmenitiesScore)
            ));

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<List<MapAmenityDto>> GetMapAmenitiesAsync(
        double minLat,
        double minLon,
        double maxLat,
        double maxLon,
        List<string>? types = null,
        CancellationToken cancellationToken = default)
    {
        return await _amenityClient.GetAmenitiesInBboxAsync(minLat, minLon, maxLat, maxLon, types, cancellationToken);
    }

    public async Task<List<MapOverlayDto>> GetMapOverlaysAsync(
        double minLat,
        double minLon,
        double maxLat,
        double maxLon,
        MapOverlayMetric metric,
        CancellationToken cancellationToken = default)
    {
        if (metric == MapOverlayMetric.PricePerSquareMeter)
        {
            return await GetPricePerSquareMeterOverlaysAsync(minLat, minLon, maxLat, maxLon, cancellationToken);
        }

        return await _cbsGeoClient.GetNeighborhoodOverlaysAsync(minLat, minLon, maxLat, maxLon, metric, cancellationToken);
    }

    private async Task<List<MapOverlayDto>> GetPricePerSquareMeterOverlaysAsync(
        double minLat, double minLon, double maxLat, double maxLon, CancellationToken ct)
    {
        var overlays = await _cbsGeoClient.GetNeighborhoodOverlaysAsync(minLat, minLon, maxLat, maxLon, MapOverlayMetric.PopulationDensity, ct);

        var listingsInBbox = await _context.Listings
            .Where(l => l.Latitude >= minLat && l.Latitude <= maxLat &&
                        l.Longitude >= minLon && l.Longitude <= maxLon &&
                        l.Price.HasValue && l.LivingAreaM2.HasValue && l.LivingAreaM2 > 0)
            .Select(l => new ListingOverlayProjection(
                l.Latitude,
                l.Longitude,
                l.Features,
                (double)(l.Price!.Value / l.LivingAreaM2!.Value)))
            .ToListAsync(ct);

        var listingsByNeighborhood = BuildNeighborhoodListingLookup(overlays, listingsInBbox);

        var results = new List<MapOverlayDto>(overlays.Count);
        foreach (var overlay in overlays)
        {
            listingsByNeighborhood.TryGetValue(overlay.Id, out var prices);
            prices ??= [];

            var medianPrice = TryCalculateMedian(prices);
            var averagePrice = prices.Count > 0 ? prices.Average() : (double?)null;
            var hasSufficientData = prices.Count >= MinimumSampleSize;

            var metricValue = medianPrice ?? averagePrice ?? 0;
            var displayValue = BuildPrimaryDisplayValue(medianPrice, averagePrice, prices.Count, hasSufficientData);
            var secondaryDisplayValue = averagePrice.HasValue ? $"Mean: € {averagePrice.Value:N0} / m²" : null;

            results.Add(overlay with
            {
                MetricName = "PricePerSquareMeter",
                MetricValue = metricValue,
                DisplayValue = displayValue,
                SecondaryMetricValue = averagePrice,
                SecondaryDisplayValue = secondaryDisplayValue,
                SampleSize = prices.Count,
                HasSufficientData = hasSufficientData
            });
        }

        return results;
    }

    private sealed record ListingOverlayProjection(
        double? Latitude,
        double? Longitude,
        Dictionary<string, string> Features,
        double PricePerSquareMeter);

    private static Dictionary<string, List<double>> BuildNeighborhoodListingLookup(
        IReadOnlyCollection<MapOverlayDto> overlays,
        IReadOnlyCollection<ListingOverlayProjection> listings)
    {
        var overlayCodes = overlays.Select(o => o.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var listingsByNeighborhood = new Dictionary<string, List<double>>(StringComparer.OrdinalIgnoreCase);
        var overlayPolygons = overlays
            .Select(overlay => new
            {
                overlay.Id,
                Polygon = TryReadPolygon(overlay.GeoJson)
            })
            .Where(x => x.Polygon != null)
            .ToDictionary(x => x.Id, x => x.Polygon!, StringComparer.OrdinalIgnoreCase);

        foreach (var listing in listings)
        {
            var mappedNeighborhood = TryReadNeighborhoodCodeFromFeatures(listing.Features, overlayCodes);
            if (mappedNeighborhood == null)
            {
                mappedNeighborhood = TryFindNeighborhoodByPoint(
                    listing.Latitude,
                    listing.Longitude,
                    overlayPolygons);
            }

            if (mappedNeighborhood == null)
            {
                continue;
            }

            if (!listingsByNeighborhood.TryGetValue(mappedNeighborhood, out var values))
            {
                values = [];
                listingsByNeighborhood[mappedNeighborhood] = values;
            }

            values.Add(listing.PricePerSquareMeter);
        }

        return listingsByNeighborhood;
    }

    private static string BuildPrimaryDisplayValue(double? medianPrice, double? averagePrice, int sampleSize, bool hasSufficientData)
    {
        if (!medianPrice.HasValue && !averagePrice.HasValue)
        {
            return "Insufficient data";
        }

        var primary = medianPrice ?? averagePrice!.Value;
        if (!hasSufficientData)
        {
            return $"Low confidence ({sampleSize} listings): median € {primary:N0} / m²";
        }

        return $"Median: € {primary:N0} / m² ({sampleSize} listings)";
    }

    private static double? TryCalculateMedian(List<double> values)
    {
        if (values.Count == 0)
        {
            return null;
        }

        var sorted = values.OrderBy(x => x).ToList();
        var middle = sorted.Count / 2;
        if (sorted.Count % 2 == 0)
        {
            return (sorted[middle - 1] + sorted[middle]) / 2d;
        }

        return sorted[middle];
    }

    private static string? TryReadNeighborhoodCodeFromFeatures(
        Dictionary<string, string>? features,
        HashSet<string> overlayCodes)
    {
        if (features == null || features.Count == 0)
        {
            return null;
        }

        foreach (var key in new[] { "buurtcode", "neighborhoodCode", "neighbourhoodCode", "cbsBuurtCode" })
        {
            if (!features.TryGetValue(key, out var code) || string.IsNullOrWhiteSpace(code))
            {
                continue;
            }

            var normalizedCode = code.Trim().ToUpperInvariant();
            if (overlayCodes.Contains(normalizedCode))
            {
                return normalizedCode;
            }
        }

        return null;
    }

    private static string? TryFindNeighborhoodByPoint(
        double? latitude,
        double? longitude,
        Dictionary<string, List<(double Lon, double Lat)>> overlayPolygons)
    {
        if (!latitude.HasValue || !longitude.HasValue)
        {
            return null;
        }

        foreach (var (overlayCode, polygon) in overlayPolygons)
        {
            if (polygon.Count > 2 && IsPointInPolygon(longitude.Value, latitude.Value, polygon))
            {
                return overlayCode;
            }
        }

        return null;
    }

    private static List<(double Lon, double Lat)>? TryReadPolygon(JsonElement feature)
    {
        if (!feature.TryGetProperty("geometry", out var geometry)
            || !geometry.TryGetProperty("type", out var geometryType)
            || geometryType.ValueKind != JsonValueKind.String
            || !geometry.TryGetProperty("coordinates", out var coordinates))
        {
            return null;
        }

        return geometryType.GetString() switch
        {
            "Polygon" => TryReadPolygonRing(coordinates),
            "MultiPolygon" => TryReadFirstPolygonFromMultiPolygon(coordinates),
            _ => null
        };
    }

    private static List<(double Lon, double Lat)>? TryReadFirstPolygonFromMultiPolygon(JsonElement coordinates)
    {
        if (coordinates.ValueKind != JsonValueKind.Array || coordinates.GetArrayLength() == 0)
        {
            return null;
        }

        return TryReadPolygonRing(coordinates[0]);
    }

    private static List<(double Lon, double Lat)>? TryReadPolygonRing(JsonElement coordinates)
    {
        if (coordinates.ValueKind != JsonValueKind.Array || coordinates.GetArrayLength() == 0)
        {
            return null;
        }

        var outerRing = coordinates[0];
        if (outerRing.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var points = new List<(double Lon, double Lat)>();
        foreach (var point in outerRing.EnumerateArray())
        {
            if (point.ValueKind != JsonValueKind.Array || point.GetArrayLength() < 2)
            {
                continue;
            }

            points.Add((point[0].GetDouble(), point[1].GetDouble()));
        }

        return points.Count >= 3 ? points : null;
    }

    private static bool IsPointInPolygon(double pointLon, double pointLat, List<(double Lon, double Lat)> polygon)
    {
        var isInside = false;
        for (var i = 0; i < polygon.Count; i++)
        {
            var j = (i - 1 + polygon.Count) % polygon.Count;

            var yi = polygon[i].Lat;
            var yj = polygon[j].Lat;
            var xi = polygon[i].Lon;
            var xj = polygon[j].Lon;

            var intersects = ((yi > pointLat) != (yj > pointLat))
                && (pointLon < ((xj - xi) * (pointLat - yi) / ((yj - yi) + double.Epsilon) + xi));
            if (intersects)
            {
                isInside = !isInside;
            }
        }

        return isInside;
    }
}
