using Valora.Application.DTOs.Map;

namespace Valora.UnitTests.DTOs;

public class MapCityInsightDtoTests
{
    [Fact]
    public void MapCityInsightDto_ShouldStoreValuesCorrectly()
    {
        // Arrange
        var city = "TestCity";
        var count = 10;
        var lat = 52.0;
        var lon = 5.0;
        var comp = 85.5;
        var safe = 90.0;
        var social = 70.0;
        var amen = 80.0;

        // Act
        var dto = new MapCityInsightDto(city, count, lat, lon, comp, safe, social, amen);

        // Assert
        Assert.Equal(city, dto.City);
        Assert.Equal(count, dto.Count);
        Assert.Equal(lat, dto.Latitude);
        Assert.Equal(lon, dto.Longitude);
        Assert.Equal(comp, dto.CompositeScore);
        Assert.Equal(safe, dto.SafetyScore);
        Assert.Equal(social, dto.SocialScore);
        Assert.Equal(amen, dto.AmenitiesScore);
    }
}
