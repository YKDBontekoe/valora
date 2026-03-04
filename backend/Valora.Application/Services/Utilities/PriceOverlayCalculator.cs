using System.Globalization;
using Valora.Application.Common.Utilities;
using Valora.Application.DTOs.Map;
using Valora.Domain.Entities;

namespace Valora.Application.Services.Utilities;

/// <summary>
/// Provides utility methods for combining real estate price data with geographical map overlays.
/// </summary>
public static class PriceOverlayCalculator
{
    /// <summary>
    /// Projects average listing prices onto map overlays by checking spatial intersections.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Why Server-Side Intersection?</strong><br/>
    /// The database (via EF Core) currently stores Listing entities with Latitude/Longitude points, but does not natively link them to neighborhood geometries.
    /// To calculate an average price per square meter per neighborhood (Overlay), we must perform point-in-polygon checks. Doing this in the application layer
    /// avoids complex spatial SQL queries (PostGIS) which would add a heavy dependency and slow down query execution.
    /// </para>
    /// <para>
    /// <strong>Performance Considerations:</strong>
    /// The spatial check (`GeoUtils.IsPointInPolygon`) is currently performed for every listing against every overlay (O(N * M)).
    /// Since the number of listings in a given view boundary is usually small (in the hundreds), this is acceptable.
    /// For larger, city-wide boundaries, a spatial index or database-level relationship (e.g., storing the NeighborhoodId on the Listing) would be necessary.
    /// </para>
    /// </remarks>
    /// <param name="baseOverlays">The neighborhood geometries (GeoJSON) used as boundaries.</param>
    /// <param name="listings">The real estate listings containing prices and areas.</param>
    /// <returns>A modified list of overlays with updated `MetricValue` representing the average price per m2.</returns>
    public static List<MapOverlayDto> CalculateAveragePriceOverlay(
        IEnumerable<MapOverlayDto> baseOverlays,
        IEnumerable<ListingPriceData> listings)
    {
        if (baseOverlays == null)
        {
            throw new ArgumentNullException(nameof(baseOverlays));
        }

        if (listings == null)
        {
            throw new ArgumentNullException(nameof(listings));
        }

        var listingsList = listings.ToList();

        return baseOverlays.Select(overlay =>
        {
            var geometry = GeoUtils.ParseGeometry(overlay.GeoJson);
            var neighborhoodListings = listingsList.Where(l =>
                l.Latitude.HasValue && l.Longitude.HasValue &&
                GeoUtils.IsPointInPolygon(l.Latitude.Value, l.Longitude.Value, geometry));

            var avgPrice = CalculateAveragePrice(neighborhoodListings);

            string displayValue = "No listing data";
            if (avgPrice.HasValue)
            {
                var formatted = avgPrice.Value.ToString("N0", CultureInfo.CurrentCulture);
                displayValue = $"€ {formatted} / m²";
            }
            var metricValue = avgPrice ?? 0;

            return overlay with
            {
                MetricName = "PricePerSquareMeter",
                MetricValue = metricValue,
                DisplayValue = displayValue
            };
        }).ToList();
    }

    private static double? CalculateAveragePrice(IEnumerable<ListingPriceData> listings)
    {
        var validListings = listings
            .Where(l => l.Price.HasValue && l.LivingAreaM2.HasValue && l.LivingAreaM2.Value > 0)
            .ToList();

        if (validListings.Count == 0)
        {
            return null;
        }

        return (double?)validListings.Average(l => l.Price!.Value / l.LivingAreaM2!.Value);
    }
}
