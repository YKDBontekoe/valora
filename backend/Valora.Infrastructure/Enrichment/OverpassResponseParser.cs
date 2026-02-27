using System.Text.Json;
using Valora.Application.DTOs;
using Valora.Application.DTOs.Map;
using Valora.Domain.Common;

namespace Valora.Infrastructure.Enrichment;

internal static class OverpassResponseParser
{
    public static AmenityStatsDto ParseAmenityStats(IEnumerable<OverpassElement> elements, ResolvedLocationDto location)
    {
        var schoolCount = 0;
        var supermarketCount = 0;
        var parkCount = 0;
        var healthcareCount = 0;
        var transitCount = 0;
        var chargingStationCount = 0;
        double? nearestDistance = null;

        foreach (var element in elements)
        {
            if (!TryGetCoordinates(element, out var lat, out var lon)) continue;

            var distance = GeoDistance.BetweenMeters(location.Latitude, location.Longitude, lat, lon);
            if (!nearestDistance.HasValue || distance < nearestDistance.Value)
            {
                nearestDistance = distance;
            }

            var tags = GetTags(element);
            var category = CategorizeAmenity(tags);

            switch (category)
            {
                case "school": schoolCount++; break;
                case "supermarket": supermarketCount++; break;
                case "park": parkCount++; break;
                case "healthcare": healthcareCount++; break;
                case "charging_station": chargingStationCount++; break;
                case "transit": transitCount++; break;
            }
        }

        var populatedCategoryCount = new[] { schoolCount, supermarketCount, parkCount, healthcareCount, transitCount, chargingStationCount }.Count(c => c > 0);
        var diversityScore = populatedCategoryCount / 6d * 100d;

        return new AmenityStatsDto(
            SchoolCount: schoolCount,
            SupermarketCount: supermarketCount,
            ParkCount: parkCount,
            HealthcareCount: healthcareCount,
            TransitStopCount: transitCount,
            NearestAmenityDistanceMeters: nearestDistance,
            DiversityScore: diversityScore,
            RetrievedAtUtc: DateTimeOffset.UtcNow,
            ChargingStationCount: chargingStationCount);
    }

    public static List<MapAmenityDto> ParseMapAmenities(IEnumerable<OverpassElement> elements)
    {
        var list = new List<MapAmenityDto>();
        foreach (var element in elements)
        {
            if (!TryGetCoordinates(element, out var lat, out var lon)) continue;

            var tags = GetTags(element);
            var name = tags.GetValueOrDefault("name") ?? tags.GetValueOrDefault("operator") ?? "Amenity";
            var type = GetAmenityType(tags);
            var id = element.Id != 0 ? element.Id.ToString() : Guid.NewGuid().ToString();

            list.Add(new MapAmenityDto(id, type, name, lat, lon, tags));
        }
        return list;
    }

    private static string GetAmenityType(Dictionary<string, string> tags)
    {
        var category = CategorizeAmenity(tags);
        return category != "unknown" ? category : "other";
    }

    private static string CategorizeAmenity(Dictionary<string, string> tags)
    {
        if (tags.TryGetValue("amenity", out var a))
        {
            if (a == "school") return "school";
            if (a is "hospital" or "clinic" or "doctors" or "pharmacy") return "healthcare";
            if (a == "charging_station") return "charging_station";
        }
        if (tags.TryGetValue("shop", out var s) && s == "supermarket") return "supermarket";
        if (tags.TryGetValue("leisure", out var l) && l == "park") return "park";
        if (tags.TryGetValue("highway", out var h) && h == "bus_stop") return "transit";
        if (tags.TryGetValue("railway", out var r) && r == "station") return "transit";

        return "unknown";
    }

    private static Dictionary<string, string> GetTags(OverpassElement element)
    {
        var tags = new Dictionary<string, string>();
        if (element.Tags.HasValue && element.Tags.Value.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in element.Tags.Value.EnumerateObject())
            {
                if (prop.Value.ValueKind == JsonValueKind.String)
                {
                    tags[prop.Name] = prop.Value.GetString()!;
                }
            }
        }
        return tags;
    }

    private static bool TryGetCoordinates(OverpassElement element, out double latitude, out double longitude)
    {
        latitude = 0;
        longitude = 0;

        if (element.Lat.HasValue && element.Lon.HasValue)
        {
            latitude = element.Lat.Value;
            longitude = element.Lon.Value;
            return true;
        }

        if (element.Center is not null)
        {
            latitude = element.Center.Lat;
            longitude = element.Center.Lon;
            return true;
        }

        return false;
    }
}
