using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Valora.Application.DTOs;
using Valora.Application.Enrichment;
using Valora.Infrastructure.Enrichment;

namespace Valora.UnitTests.Enrichment;

public class OverpassAmenityClientTests
{
    private readonly Mock<ILogger<OverpassAmenityClient>> _loggerMock;
    private readonly IMemoryCache _cache;
    private readonly IOptions<ContextEnrichmentOptions> _options;
    private readonly ResolvedLocationDto _location;

    public OverpassAmenityClientTests()
    {
        _loggerMock = new Mock<ILogger<OverpassAmenityClient>>();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _options = Options.Create(new ContextEnrichmentOptions
        {
            OverpassBaseUrl = "https://overpass-api.de"
        });
        _location = new ResolvedLocationDto(
            Query: "Damrak 1 Amsterdam",
            DisplayAddress: "Damrak 1, 1012LG Amsterdam",
            Latitude: 52.37714,
            Longitude: 4.89803,
            RdX: 121691,
            RdY: 487809,
            MunicipalityCode: "GM0363",
            MunicipalityName: "Amsterdam",
            DistrictCode: "WK0363AD",
            DistrictName: "Burgwallen-Nieuwe Zijde",
            NeighborhoodCode: "BU0363AD03",
            NeighborhoodName: "Nieuwendijk-Noord",
            PostalCode: "1012LG");
    }

    [Fact]
    public async Task GetAmenitiesAsync_ParsesAllTagsCorrectly()
    {
        // Arrange
        var elements = new[]
        {
            new { lat = 52.377, lon = 4.898, tags = new Dictionary<string, string> { ["amenity"] = "school" } },
            new { lat = 52.378, lon = 4.899, tags = new Dictionary<string, string> { ["shop"] = "supermarket" } },
            new { lat = 52.379, lon = 4.900, tags = new Dictionary<string, string> { ["leisure"] = "park" } },
            new { lat = 52.380, lon = 4.901, tags = new Dictionary<string, string> { ["amenity"] = "hospital" } },
            new { lat = 52.381, lon = 4.902, tags = new Dictionary<string, string> { ["highway"] = "bus_stop" } },
            new { lat = 52.382, lon = 4.903, tags = new Dictionary<string, string> { ["railway"] = "station" } },
            // Multiple tags in one element
            new { lat = 52.383, lon = 4.904, tags = new Dictionary<string, string> { ["amenity"] = "pharmacy", ["highway"] = "something_else" } }
        };

        var jsonResponse = JsonSerializer.Serialize(new { elements });
        var handlerMock = CreateHandlerMock(HttpStatusCode.OK, jsonResponse);
        var httpClient = new HttpClient(handlerMock.Object);
        var client = new OverpassAmenityClient(httpClient, _cache, _options, _loggerMock.Object);

        // Act
        var result = await client.GetAmenitiesAsync(_location, 1000);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.SchoolCount);
        Assert.Equal(1, result.SupermarketCount);
        Assert.Equal(1, result.ParkCount);
        Assert.Equal(2, result.HealthcareCount); // hospital + pharmacy
        Assert.Equal(2, result.TransitStopCount); // bus_stop + station
        Assert.True(result.DiversityScore > 0);
        Assert.NotNull(result.NearestAmenityDistanceMeters);
    }

    [Fact]
    public async Task GetAmenitiesAsync_HandlesMissingTagsAndNonStringValues()
    {
        // Arrange
        var jsonResponse = """
        {
          "elements": [
            { "lat": 52.377, "lon": 4.898 },
            { "lat": 52.377, "lon": 4.898, "tags": null },
            { "lat": 52.377, "lon": 4.898, "tags": { "amenity": 123, "shop": "supermarket" } }
          ]
        }
        """;
        var handlerMock = CreateHandlerMock(HttpStatusCode.OK, jsonResponse);
        var httpClient = new HttpClient(handlerMock.Object);
        var client = new OverpassAmenityClient(httpClient, _cache, _options, _loggerMock.Object);

        // Act
        var result = await client.GetAmenitiesAsync(_location, 1000);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.SchoolCount);
        Assert.Equal(1, result.SupermarketCount);
    }

    [Fact]
    public async Task GetAmenitiesAsync_CachesResults()
    {
        // Arrange
        var jsonResponse = JsonSerializer.Serialize(new { elements = new[] { new { lat = 52.377, lon = 4.898, tags = new Dictionary<string, string> { ["amenity"] = "school" } } } });
        var handlerMock = CreateHandlerMock(HttpStatusCode.OK, jsonResponse);
        var httpClient = new HttpClient(handlerMock.Object);
        var client = new OverpassAmenityClient(httpClient, _cache, _options, _loggerMock.Object);

        // Act
        var result1 = await client.GetAmenitiesAsync(_location, 1000);
        var result2 = await client.GetAmenitiesAsync(_location, 1000);

        // Assert
        Assert.Same(result1, result2);
        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    private static Mock<HttpMessageHandler> CreateHandlerMock(HttpStatusCode statusCode, string content)
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock
           .Protected()
           .Setup<Task<HttpResponseMessage>>(
              "SendAsync",
              ItExpr.IsAny<HttpRequestMessage>(),
              ItExpr.IsAny<CancellationToken>()
           )
           .ReturnsAsync(new HttpResponseMessage()
           {
               StatusCode = statusCode,
               Content = new StringContent(content, Encoding.UTF8, "application/json"),
           });
        return handlerMock;
    }
}
