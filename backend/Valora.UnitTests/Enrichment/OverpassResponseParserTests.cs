using System.Text.Json;
using Valora.Application.DTOs;
using Valora.Infrastructure.Enrichment;

namespace Valora.UnitTests.Enrichment;

public class OverpassResponseParserTests
{
    private static OverpassElement CreateElement(long id, double? lat, double? lon, object? tags = null, double? centerLat = null, double? centerLon = null)
    {
        JsonElement? tagsElement = null;
        if (tags != null)
        {
            var json = JsonSerializer.Serialize(tags);
            using var doc = JsonDocument.Parse(json);
            tagsElement = doc.RootElement.Clone();
        }

        OverpassCenter? center = null;
        if (centerLat.HasValue && centerLon.HasValue)
        {
            center = new OverpassCenter(centerLat.Value, centerLon.Value);
        }

        return new OverpassElement("node", id, lat, lon, center, tagsElement);
    }

    [Fact]
    public void ParseAmenityStats_CalculatesCountsCorrectly()
    {
        // Arrange
        var location = new ResolvedLocationDto("q", "addr", 52.0, 4.0, 0, 0, null, null, null, null, null, null, null);
        var elements = new[]
        {
            CreateElement(1, 52.001, 4.001, new { amenity = "school" }),
            CreateElement(2, 52.002, 4.002, new { shop = "supermarket" }),
            CreateElement(3, 52.003, 4.003, new { leisure = "park" }),
            CreateElement(4, 52.004, 4.004, new { amenity = "hospital" }), // healthcare
            CreateElement(5, 52.005, 4.005, new { highway = "bus_stop" }), // transit
            CreateElement(6, 52.006, 4.006, new { railway = "station" }), // transit
            CreateElement(7, 52.007, 4.007, new { amenity = "charging_station" }),
            CreateElement(8, 52.008, 4.008, new { amenity = "unknown" }) // Ignored
        };

        // Act
        var result = OverpassResponseParser.ParseAmenityStats(elements, location);

        // Assert
        Assert.Equal(1, result.SchoolCount);
        Assert.Equal(1, result.SupermarketCount);
        Assert.Equal(1, result.ParkCount);
        Assert.Equal(1, result.HealthcareCount);
        Assert.Equal(2, result.TransitStopCount);
        Assert.Equal(1, result.ChargingStationCount);
        Assert.Equal(100, result.DiversityScore);
    }

    [Theory]
    [InlineData("clinic")]
    [InlineData("doctors")]
    [InlineData("pharmacy")]
    public void ParseAmenityStats_RecognizesAllHealthcareTypes(string amenityType)
    {
        // Arrange
        var location = new ResolvedLocationDto("q", "addr", 52.0, 4.0, 0, 0, null, null, null, null, null, null, null);
        var elements = new[]
        {
            CreateElement(1, 52.001, 4.001, new { amenity = amenityType })
        };

        // Act
        var result = OverpassResponseParser.ParseAmenityStats(elements, location);

        // Assert
        Assert.Equal(1, result.HealthcareCount);
    }

    [Fact]
    public void ParseAmenityStats_UsesCenterCoordinates()
    {
        // Arrange
        var location = new ResolvedLocationDto("q", "addr", 52.0, 4.0, 0, 0, null, null, null, null, null, null, null);
        var elements = new[]
        {
            CreateElement(1, null, null, new { amenity = "school" }, 52.001, 4.001)
        };

        // Act
        var result = OverpassResponseParser.ParseAmenityStats(elements, location);

        // Assert
        Assert.Equal(1, result.SchoolCount);
        Assert.NotNull(result.NearestAmenityDistanceMeters);
    }

    [Fact]
    public void ParseMapAmenities_ReturnsCorrectDtos()
    {
        // Arrange
        var elements = new[]
        {
            CreateElement(1, 52.0, 4.0, new { amenity = "school", name = "My School" }),
            CreateElement(2, 52.1, 4.1, new { shop = "supermarket" }) // No name, defaults to "Amenity" or "operator" if exists
        };

        // Act
        var result = OverpassResponseParser.ParseMapAmenities(elements);

        // Assert
        Assert.Equal(2, result.Count);
        var school = result.First(x => x.Type == "school");
        Assert.Equal("My School", school.Name);
        Assert.Equal("1", school.Id);

        var shop = result.First(x => x.Type == "supermarket");
        Assert.Equal("Amenity", shop.Name);
    }

    [Fact]
    public void ParseMapAmenities_FallsBackToOperatorName()
    {
        // Arrange
        var elements = new[]
        {
            CreateElement(1, 52.0, 4.0, new { amenity = "charging_station", @operator = "Shell" })
        };

        // Act
        var result = OverpassResponseParser.ParseMapAmenities(elements);

        // Assert
        var item = result.Single();
        Assert.Equal("Shell", item.Name);
    }
}
