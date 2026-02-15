using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Valora.Application.DTOs;
using Valora.Application.DTOs.Map;
using Valora.Application.Enrichment;
using Valora.Application.Common.Interfaces;
using Valora.Infrastructure.Enrichment;
using Valora.Domain.Entities;
using Xunit;

namespace Valora.UnitTests.Enrichment;

public class OverpassAmenityClientTests
{
    private readonly Mock<ILogger<OverpassAmenityClient>> _loggerMock;
    private readonly Mock<IContextCacheRepository> _dbCacheMock;
    private readonly IMemoryCache _cache;
    private readonly IOptions<ContextEnrichmentOptions> _options;
    private readonly ResolvedLocationDto _location;

    public OverpassAmenityClientTests()
    {
        _loggerMock = new Mock<ILogger<OverpassAmenityClient>>();
        _dbCacheMock = new Mock<IContextCacheRepository>();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _options = Options.Create(new ContextEnrichmentOptions
        {
            OverpassBaseUrl = "https://overpass-api.de",
            AmenitiesCacheMinutes = 1
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
        var elements = new[]
        {
            new { lat = 52.377, lon = 4.898, tags = new Dictionary<string, string> { ["amenity"] = "school" } },
            new { lat = 52.378, lon = 4.899, tags = new Dictionary<string, string> { ["shop"] = "supermarket" } },
            new { lat = 52.379, lon = 4.900, tags = new Dictionary<string, string> { ["leisure"] = "park" } },
            new { lat = 52.380, lon = 4.901, tags = new Dictionary<string, string> { ["amenity"] = "hospital" } },
            new { lat = 52.381, lon = 4.902, tags = new Dictionary<string, string> { ["highway"] = "bus_stop" } },
            new { lat = 52.382, lon = 4.903, tags = new Dictionary<string, string> { ["railway"] = "station" } }
        };

        var jsonResponse = JsonSerializer.Serialize(new { elements });
        var handlerMock = CreateHandlerMock(HttpStatusCode.OK, jsonResponse);
        var httpClient = new HttpClient(handlerMock.Object);
        var client = new OverpassAmenityClient(httpClient, _cache, _dbCacheMock.Object, _options, _loggerMock.Object);

        var result = await client.GetAmenitiesAsync(_location, 1000);

        Assert.NotNull(result);
        Assert.Equal(1, result.SchoolCount);
        Assert.Equal(1, result.SupermarketCount);
        Assert.Equal(1, result.ParkCount);
    }

    [Fact]
    public async Task GetAmenitiesAsync_WhenMemCacheMiss_ChecksDbCache()
    {
        var location = new ResolvedLocationDto("q", "A", 52.37, 4.89, null, null, null, null, null, null, null, null, null);
        var locationKey = "52.37000:4.89000:1000";
        var dbCache = new AmenityCache { LocationKey = locationKey, SchoolCount = 10, ExpiresAtUtc = DateTimeOffset.UtcNow.AddHours(1) };

        _dbCacheMock.Setup(x => x.GetAmenityCacheAsync(locationKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dbCache);

        var client = new OverpassAmenityClient(new HttpClient(), _cache, _dbCacheMock.Object, _options, _loggerMock.Object);
        var result = await client.GetAmenitiesAsync(location, 1000);

        Assert.NotNull(result);
        Assert.Equal(10, result.SchoolCount);
        _dbCacheMock.Verify(x => x.GetAmenityCacheAsync(locationKey, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAmenitiesAsync_WhenHttpFails_Throws()
    {
        var handlerMock = CreateHandlerMock(HttpStatusCode.InternalServerError, "{}");
        var httpClient = new HttpClient(handlerMock.Object);
        var client = new OverpassAmenityClient(httpClient, _cache, _dbCacheMock.Object, _options, _loggerMock.Object);

        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAmenitiesAsync(_location, 1000));
    }

    [Fact]
    public async Task GetAmenitiesInBboxAsync_ReturnsParsedAmenities()
    {
        var elements = new[]
        {
            new { id = 123, lat = 52.0, lon = 4.0, tags = new Dictionary<string, string> { ["amenity"] = "charging_station", ["name"] = "Fast Charge" } }
        };

        var jsonResponse = JsonSerializer.Serialize(new { elements });
        var handlerMock = CreateHandlerMock(HttpStatusCode.OK, jsonResponse);
        var httpClient = new HttpClient(handlerMock.Object);
        var client = new OverpassAmenityClient(httpClient, _cache, _dbCacheMock.Object, _options, _loggerMock.Object);

        var result = await client.GetAmenitiesInBboxAsync(52.0, 4.0, 52.5, 4.5);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("charging_station", result[0].Type);
    }

    private Mock<HttpMessageHandler> CreateHandlerMock(HttpStatusCode statusCode, string content)
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(content, Encoding.UTF8, "application/json")
            });
        return handlerMock;
    }
}
