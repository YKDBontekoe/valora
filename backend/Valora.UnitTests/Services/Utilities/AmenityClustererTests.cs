using Valora.Application.DTOs.Map;
using Valora.Infrastructure.Services.AppServices.Utilities;
using Xunit;

namespace Valora.UnitTests.Services.Utilities;

public class AmenityClustererTests
{
    [Fact]
    public void ClusterAmenities_ClustersCorrectly_BasedOnCellSize()
    {
        // Arrange
        var amenities = new List<MapAmenityDto>
        {
            new("1", "supermarket", "Supermarket 1", 52.12, 4.12),
            new("2", "supermarket", "Supermarket 2", 52.13, 4.13), // Close to #1
            new("3", "school", "School 1", 52.14, 4.14), // Close to #1
            new("4", "park", "Park 1", 52.22, 4.22) // Far away
        };

        var cellSize = 0.05;

        // Act
        var clusters = AmenityClusterer.ClusterAmenities(amenities, cellSize);

        // Assert
        Assert.Equal(2, clusters.Count);

        // First cluster (3 amenities)
        // Grouping logic: Math.Floor(52.12 / 0.05) = 1042
        // Lat: (1042 * 0.05) + 0.025 = 52.125
        // Grouping logic: Math.Floor(4.12 / 0.05) = 82
        // Lon: (82 * 0.05) + 0.025 = 4.125
        var cluster1 = clusters.Single(c => c.Count == 3);
        Assert.Equal(52.125, Math.Round(cluster1.Latitude, 3));
        Assert.Equal(4.125, Math.Round(cluster1.Longitude, 3));
        Assert.Equal(2, cluster1.TypeCounts["supermarket"]);
        Assert.Equal(1, cluster1.TypeCounts["school"]);

        // Second cluster (1 amenity)
        var cluster2 = clusters.Single(c => c.Count == 1);
        Assert.Equal(1, cluster2.TypeCounts["park"]);
    }

    [Fact]
    public void ClusterAmenities_ReturnsEmpty_WhenNoAmenities()
    {
        // Arrange
        var amenities = new List<MapAmenityDto>();
        var cellSize = 0.05;

        // Act
        var clusters = AmenityClusterer.ClusterAmenities(amenities, cellSize);

        // Assert
        Assert.Empty(clusters);
    }
}
