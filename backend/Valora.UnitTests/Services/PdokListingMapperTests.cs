using System.Text.Json;
using Valora.Infrastructure.Services;
using Xunit;

namespace Valora.UnitTests.Services;

public class PdokListingMapperTests
{
    private readonly PdokListingMapper _mapper;

    public PdokListingMapperTests()
    {
        _mapper = new PdokListingMapper();
    }

    [Fact]
    public void MapToDto_WithValidData_ReturnsMappedDto()
    {
        // Arrange
        var json = @"
        {
            ""weergavenaam"": ""Kerkstraat 1, Amsterdam"",
            ""woonplaatsnaam"": ""Amsterdam"",
            ""postcode"": ""1017GD"",
            ""centroide_ll"": ""POINT(4.88 52.36)"",
            ""bouwjaar"": ""1900"",
            ""oppervlakte"": ""120"",
            ""gebruiksdoelverblijfsobject"": ""woonfunctie""
        }";
        using var doc = JsonDocument.Parse(json);
        var element = doc.RootElement;
        var pdokId = "test-id";

        // Act
        var result = _mapper.MapToDto(element, pdokId, null, null, null);

        // Assert
        Assert.Equal("Kerkstraat 1, Amsterdam", result.Address);
        Assert.Equal("Amsterdam", result.City);
        Assert.Equal("1017GD", result.PostalCode);
        Assert.Equal(52.36, result.Latitude);
        Assert.Equal(4.88, result.Longitude);
        Assert.Equal(1900, result.YearBuilt);
        Assert.Equal(120, result.LivingAreaM2);
        Assert.Equal("woonfunctie", result.PropertyType);
        Assert.Contains("Built in 1900", result.Description);
        Assert.Contains("Usage: woonfunctie", result.Description);
    }

    [Fact]
    public void MapToDto_WithMissingData_ReturnsPartialDto()
    {
        // Arrange
        var json = @"{}";
        using var doc = JsonDocument.Parse(json);
        var element = doc.RootElement;
        var pdokId = "test-id";

        // Act
        var result = _mapper.MapToDto(element, pdokId, null, null, null);

        // Assert
        Assert.Equal("Unknown Address", result.Address);
        Assert.Null(result.City);
        Assert.Null(result.Latitude);
        Assert.Null(result.YearBuilt);
    }

    [Fact]
    public void MapToDto_WithInvalidCoordinates_ReturnsNullCoordinates()
    {
        // Arrange
        var json = @"{ ""centroide_ll"": ""INVALID"" }";
        using var doc = JsonDocument.Parse(json);
        var element = doc.RootElement;

        // Act
        var result = _mapper.MapToDto(element, "id", null, null, null);

        // Assert
        Assert.Null(result.Latitude);
        Assert.Null(result.Longitude);
    }
}
