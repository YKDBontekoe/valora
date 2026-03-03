using System.Globalization;
using Valora.Application.Common.Utilities;
using Valora.Application.DTOs.Map;
using Valora.Domain.Entities;

namespace Valora.Application.Services.Utilities;

public static class PriceOverlayCalculator
{
    public static List<MapOverlayDto> CalculateAveragePriceOverlay(
        IEnumerable<MapOverlayDto> baseOverlays,
        IEnumerable<ListingPriceData> listings)
    {
        return baseOverlays.Select(overlay =>
        {
            var geometry = GeoUtils.ParseGeometry(overlay.GeoJson);
            var neighborhoodListings = listings.Where(l =>
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
