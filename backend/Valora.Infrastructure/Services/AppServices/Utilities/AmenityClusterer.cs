using Valora.Application.DTOs.Map;

namespace Valora.Infrastructure.Services.AppServices.Utilities;

/// <summary>
/// Provides utility methods for clustering map amenities to prevent client-side rendering bottlenecks.
/// </summary>
public static class AmenityClusterer
{
    /// <summary>
    /// Groups individual amenities into spatial clusters based on a grid system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Why Grid-Based Clustering?</strong><br/>
    /// Instead of distance-based clustering algorithms (like DBSCAN or K-Means) which typically have O(N^2) or O(N*K) complexity,
    /// we use a discrete grid-based approach. By mapping each amenity's coordinate to a grid cell index using integer division,
    /// the grouping operation runs in O(N) time. This is critical for performance when processing tens of thousands of amenities (e.g., all trees in Amsterdam).
    /// </para>
    /// <para>
    /// <strong>Trade-offs:</strong>
    /// The resulting clusters are locked to rigid grid cell centers rather than the geometric centroid of the grouped points.
    /// This causes slight visual snapping when zooming, but avoids expensive centroid calculations and guarantees stable, non-overlapping clusters.
    /// </para>
    /// </remarks>
    /// <param name="amenities">The raw list of amenities to cluster.</param>
    /// <param name="cellSize">The size of the grid cell in coordinate degrees. Defines the visual density of clusters.</param>
    /// <returns>A list of aggregated clusters, including total count and breakdown by amenity type.</returns>
    public static List<MapAmenityClusterDto> ClusterAmenities(IEnumerable<MapAmenityDto> amenities, double cellSize)
    {
        if (amenities == null)
        {
            throw new ArgumentNullException(nameof(amenities));
        }

        if (cellSize <= 0 || double.IsNaN(cellSize) || double.IsInfinity(cellSize))
        {
            throw new ArgumentException("Cell size must be a positive, finite number.", nameof(cellSize));
        }

        return amenities
            .GroupBy(amenity => (
                Lat: Math.Floor(amenity.Latitude / cellSize),
                Lon: Math.Floor(amenity.Longitude / cellSize)
            ))
            .Select(groupedAmenities =>
            {
                var count = groupedAmenities.Count();
                var lat = (groupedAmenities.Key.Lat * cellSize) + (cellSize / 2);
                var lon = (groupedAmenities.Key.Lon * cellSize) + (cellSize / 2);

                var typeCounts = groupedAmenities.GroupBy(amenity => amenity.Type)
                    .ToDictionary(typeGroup => typeGroup.Key, typeGroup => typeGroup.Count());

                return new MapAmenityClusterDto(lat, lon, count, typeCounts);
            })
            .ToList();
    }
}
