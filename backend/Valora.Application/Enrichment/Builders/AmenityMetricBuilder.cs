using Valora.Application.DTOs;

namespace Valora.Application.Enrichment.Builders;

public class AmenityMetricBuilder
{
    public List<ContextMetricDto> Build(AmenityStatsDto? amenities, List<string> warnings)
    {
        if (amenities is null)
        {
            warnings.Add("OSM amenities were unavailable; amenity score is partial.");
            return [];
        }

        var proximityScore = ScoreAmenityProximity(amenities.NearestAmenityDistanceMeters);
        var countScore = ScoreAmenityCount(amenities);

        return
        [
            new("schools", "Schools in Radius", amenities.SchoolCount, "count", null, "OpenStreetMap / Overpass"),
            new("supermarkets", "Supermarkets in Radius", amenities.SupermarketCount, "count", null, "OpenStreetMap / Overpass"),
            new("parks", "Parks in Radius", amenities.ParkCount, "count", null, "OpenStreetMap / Overpass"),
            new("healthcare", "Healthcare in Radius", amenities.HealthcareCount, "count", null, "OpenStreetMap / Overpass"),
            new("transit_stops", "Transit Stops in Radius", amenities.TransitStopCount, "count", null, "OpenStreetMap / Overpass"),
            new("amenity_diversity", "Amenity Diversity", amenities.DiversityScore, "score", amenities.DiversityScore, "OpenStreetMap / Overpass"),
            new("amenity_proximity", "Nearest Amenity Distance", amenities.NearestAmenityDistanceMeters, "m", proximityScore, "OpenStreetMap / Overpass"),
            new("amenity_count_score", "Amenity Volume Score", countScore, "score", countScore, "OpenStreetMap / Overpass")
        ];
    }

    /// <summary>
    /// Scores the volume of amenities in the search radius.
    /// Simple quantity heuristic: 20 amenities = 100 score.
    /// </summary>
    private static double ScoreAmenityCount(AmenityStatsDto amenities)
    {
        var total = amenities.SchoolCount + amenities.SupermarketCount + amenities.ParkCount + amenities.HealthcareCount + amenities.TransitStopCount;
        return Math.Clamp(total * 5, 0, 100);
    }

    /// <summary>
    /// Scores the "15-minute city" potential based on proximity to the nearest key amenity.
    /// </summary>
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
}
