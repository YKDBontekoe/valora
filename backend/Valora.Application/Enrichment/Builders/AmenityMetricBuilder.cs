using Valora.Application.Common.Constants;
using Valora.Application.DTOs;

namespace Valora.Application.Enrichment.Builders;

public static class AmenityMetricBuilder
{
    public static List<ContextMetricDto> Build(AmenityStatsDto? amenities, NeighborhoodStatsDto? cbs, List<string> warnings)
    {
        var metrics = new List<ContextMetricDto>();

        if (amenities != null)
        {
            var proximityScore = ScoreAmenityProximity(amenities.NearestAmenityDistanceMeters);
            var countScore = ScoreAmenityCount(amenities);

            metrics.AddRange(
            [
                new("schools", "Schools in Radius", amenities.SchoolCount, "count", null, DataSources.OpenStreetMap),
                new("supermarkets", "Supermarkets in Radius", amenities.SupermarketCount, "count", null, DataSources.OpenStreetMap),
                new("parks", "Parks in Radius", amenities.ParkCount, "count", null, DataSources.OpenStreetMap),
                new("healthcare", "Healthcare in Radius", amenities.HealthcareCount, "count", null, DataSources.OpenStreetMap),
                new("transit_stops", "Transit Stops in Radius", amenities.TransitStopCount, "count", null, DataSources.OpenStreetMap),
                new("charging_stations", "Charging Stations in Radius", amenities.ChargingStationCount, "count", null, DataSources.OpenStreetMap),
                new("amenity_diversity", "Amenity Diversity", amenities.DiversityScore, "score", amenities.DiversityScore, DataSources.OpenStreetMap),
                new("amenity_proximity", "Nearest Amenity Distance", amenities.NearestAmenityDistanceMeters, "m", proximityScore, DataSources.OpenStreetMap),
                new("amenity_count_score", "Amenity Volume Score", countScore, "score", countScore, DataSources.OpenStreetMap)
            ]);
        }
        else
        {
            warnings.Add("OSM amenities were unavailable; amenity score is partial.");
        }

        if (cbs != null)
        {
            // Phase 2: CBS Proximity - Walkability
            metrics.Add(new("dist_supermarket", "Dist. to Supermarket", cbs.DistanceToSupermarket, "km", ScoreProximity(cbs.DistanceToSupermarket, 1.0, 2.5), DataSources.CbsStatLine));
            metrics.Add(new("dist_gp", "Dist. to GP", cbs.DistanceToGp, "km", ScoreProximity(cbs.DistanceToGp, 1.5, 3.0), DataSources.CbsStatLine));
            metrics.Add(new("dist_school", "Dist. to School", cbs.DistanceToSchool, "km", ScoreProximity(cbs.DistanceToSchool, 1.0, 3.0), DataSources.CbsStatLine));
            metrics.Add(new("dist_daycare", "Dist. to Daycare", cbs.DistanceToDaycare, "km", null, DataSources.CbsStatLine));
            metrics.Add(new("schools_3km", "Schools within 3km", cbs.SchoolsWithin3km, "count", null, DataSources.CbsStatLine));
        }

        return metrics;
    }

    /// <summary>
    /// Scores the volume of amenities in the search radius.
    /// </summary>
    /// <remarks>
    /// Simple quantity heuristic: 20 total amenities = 100 score.
    /// This encourages diversity (e.g. 5 schools + 5 parks + 10 shops = 100).
    /// </remarks>
    private static double ScoreAmenityCount(AmenityStatsDto amenities)
    {
        var total =
            amenities.SchoolCount +
            amenities.SupermarketCount +
            amenities.ParkCount +
            amenities.HealthcareCount +
            amenities.TransitStopCount +
            amenities.ChargingStationCount;
        return Math.Clamp(total * 4, 0, 100);
    }

    /// <summary>
    /// Scores the "15-minute city" potential based on proximity to the nearest key amenity.
    /// </summary>
    /// <remarks>
    /// Based on walking speed (approx 5km/h).
    /// 250m = ~3 mins walk (Excellent).
    /// 1000m = ~12 mins walk (Acceptable/Bikeable).
    /// </remarks>
    private static double? ScoreAmenityProximity(double? nearestDistanceMeters)
    {
        if (!nearestDistanceMeters.HasValue) return null;

        return nearestDistanceMeters.Value switch
        {
            <= 250 => 100, // Very Walkable
            <= 500 => 85,  // Walkable
            <= 1000 => 70, // Bikeable
            <= 1500 => 55, // Short Drive
            <= 2000 => 40, // Drive
            _ => 25        // Isolated
        };
    }

    /// <summary>
    /// Scores proximity to key amenities (Supermarket, GP, School).
    /// </summary>
    private static double? ScoreProximity(double? distanceKm, double optimalKm, double acceptableKm)
    {
        if (!distanceKm.HasValue) return null;

        if (distanceKm <= optimalKm) return 100;
        if (distanceKm <= acceptableKm) return 70;
        return 40;
    }
}
