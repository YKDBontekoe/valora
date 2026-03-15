using System.Globalization;
using System.Text.Json;
using Valora.Application.DTOs.Map;
using Valora.Infrastructure.Services.AppServices.Utilities;
using Xunit;

namespace Valora.UnitTests.Services.Utilities;

public class PriceOverlayCalculatorTests
{
    [Fact]
    public void CalculateAveragePriceOverlay_CalculatesAveragePrice_Correctly()
    {
        // Arrange
        var jsonStr = "{\"type\":\"Polygon\",\"coordinates\":[[[4.0,52.0],[4.2,52.0],[4.2,52.2],[4.0,52.2],[4.0,52.0]]]}";
        using var jsonDoc = JsonDocument.Parse(jsonStr);

        var baseOverlays = new List<MapOverlayDto>
        {
            new("BU00000000", "Neighborhood 1", "PopulationDensity", 10.0, "10", jsonDoc.RootElement.Clone())
        };

        var listings = new List<ListingPriceData>
        {
            new ListingPriceData(400000, 100, 52.1, 4.1), // 4000 per sqm
            new ListingPriceData(600000, 100, 52.1, 4.1), // 6000 per sqm
            new ListingPriceData(1000000, 100, 52.3, 4.3) // Outside polygon
        };

        // Act
        var result = PriceOverlayCalculator.CalculateAveragePriceOverlay(baseOverlays, listings);

        // Assert
        Assert.Single(result);
        var overlay = result.First();

        Assert.Equal("PricePerSquareMeter", overlay.MetricName);
        Assert.Equal(5000.0, overlay.MetricValue);
        var expectedValue = 5000.0;
        var formatted = expectedValue.ToString("N0", CultureInfo.CurrentCulture);
        Assert.Equal($"€ {formatted} / m²", overlay.DisplayValue);
        // Compare string representations since JsonElement equality might fail on clones
        Assert.Equal(baseOverlays[0].GeoJson.ToString(), overlay.GeoJson.ToString());
        Assert.Equal(baseOverlays[0].Id, overlay.Id);
        Assert.Equal(baseOverlays[0].Name, overlay.Name);
    }

    [Fact]
    public void CalculateAveragePriceOverlay_HandlesEmptyListings_Gracefully()
    {
        // Arrange
        var jsonStr = "{\"type\":\"Polygon\",\"coordinates\":[[[4.0,52.0],[4.2,52.0],[4.2,52.2],[4.0,52.2],[4.0,52.0]]]}";
        using var jsonDoc = JsonDocument.Parse(jsonStr);

        var baseOverlays = new List<MapOverlayDto>
        {
            new("BU00000000", "Neighborhood 1", "PopulationDensity", 10.0, "10", jsonDoc.RootElement.Clone())
        };

        var listings = new List<ListingPriceData>();

        // Act
        var result = PriceOverlayCalculator.CalculateAveragePriceOverlay(baseOverlays, listings);

        // Assert
        Assert.Single(result);
        var overlay = result.First();

        Assert.Equal("PricePerSquareMeter", overlay.MetricName);
        Assert.Equal(0, overlay.MetricValue);
        Assert.Equal("No listing data", overlay.DisplayValue);
    }

    [Fact]
    public void CalculateAveragePriceOverlay_IgnoresInvalidListings()
    {
        // Arrange
        var jsonStr = "{\"type\":\"Polygon\",\"coordinates\":[[[4.0,52.0],[4.2,52.0],[4.2,52.2],[4.0,52.2],[4.0,52.0]]]}";
        using var jsonDoc = JsonDocument.Parse(jsonStr);

        var baseOverlays = new List<MapOverlayDto>
        {
            new("BU00000000", "Neighborhood 1", "PopulationDensity", 10.0, "10", jsonDoc.RootElement.Clone())
        };

        var listings = new List<ListingPriceData>
        {
            new ListingPriceData(null, 100, 52.1, 4.1), // No price
            new ListingPriceData(600000, null, 52.1, 4.1), // No area
            new ListingPriceData(600000, 0, 52.1, 4.1), // Zero area
            new ListingPriceData(400000, 100, 52.1, 4.1) // Valid (4000 per sqm)
        };

        // Act
        var result = PriceOverlayCalculator.CalculateAveragePriceOverlay(baseOverlays, listings);

        // Assert
        Assert.Single(result);
        var overlay = result.First();

        Assert.Equal("PricePerSquareMeter", overlay.MetricName);
        Assert.Equal(4000.0, overlay.MetricValue);
        var expectedValue = 4000.0;
        var formatted = expectedValue.ToString("N0", CultureInfo.CurrentCulture);
        Assert.Equal($"€ {formatted} / m²", overlay.DisplayValue);
    }
}
