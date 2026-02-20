using Valora.Infrastructure.Enrichment;

namespace Valora.UnitTests.Enrichment;

public class OverpassQueryBuilderTests
{
    [Fact]
    public void BuildAmenityQuery_IncludesCoordinatesAndRadius()
    {
        // Arrange
        double lat = 52.3702;
        double lon = 4.8952;
        int radius = 500;

        // Act
        var query = OverpassQueryBuilder.BuildAmenityQuery(lat, lon, radius);

        // Assert
        Assert.Contains($"around:{radius}", query);
        Assert.Contains(lat.ToString(System.Globalization.CultureInfo.InvariantCulture), query);
        Assert.Contains(lon.ToString(System.Globalization.CultureInfo.InvariantCulture), query);
        Assert.Contains("[amenity=school]", query);
    }

    [Fact]
    public void BuildBboxQuery_IncludesBboxAndFilters()
    {
        // Arrange
        double minLat = 52.0;
        double minLon = 4.0;
        double maxLat = 52.5;
        double maxLon = 4.5;
        var types = new List<string> { "school", "supermarket" };

        // Act
        var query = OverpassQueryBuilder.BuildBboxQuery(minLat, minLon, maxLat, maxLon, types);

        // Assert
        Assert.Contains("52,", query);
        Assert.Contains("4,", query);
        Assert.Contains("52.5,", query);
        Assert.Contains("4.5", query);
        Assert.Contains("[amenity=school]", query);
        Assert.Contains("[shop=supermarket]", query);
        Assert.DoesNotContain("[leisure=park]", query);
    }

    [Fact]
    public void BuildBboxQuery_WhenTypesNull_IncludesAll()
    {
        // Arrange
        double minLat = 52.0;
        double minLon = 4.0;
        double maxLat = 52.5;
        double maxLon = 4.5;

        // Act
        var query = OverpassQueryBuilder.BuildBboxQuery(minLat, minLon, maxLat, maxLon, null);

        // Assert
        Assert.Contains("[amenity=school]", query);
        Assert.Contains("[shop=supermarket]", query);
        Assert.Contains("[leisure=park]", query);
        Assert.Contains("[highway=bus_stop]", query);
    }
}
