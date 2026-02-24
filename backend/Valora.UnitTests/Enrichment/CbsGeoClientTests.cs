using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Application.DTOs.Map;
using Valora.Application.Enrichment;
using Valora.Infrastructure.Enrichment;

namespace Valora.UnitTests.Enrichment;

public class CbsGeoClientTests
{
    private readonly Mock<ILogger<CbsGeoClient>> _loggerMock;
    private readonly Mock<ICbsNeighborhoodStatsClient> _statsClientMock;
    private readonly Mock<ICbsCrimeStatsClient> _crimeClientMock;
    private readonly IMemoryCache _cache;
    private readonly IOptions<ContextEnrichmentOptions> _options;

    public CbsGeoClientTests()
    {
        _loggerMock = new Mock<ILogger<CbsGeoClient>>();
        _statsClientMock = new Mock<ICbsNeighborhoodStatsClient>();
        _crimeClientMock = new Mock<ICbsCrimeStatsClient>();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _options = Options.Create(new ContextEnrichmentOptions
        {
            CbsBaseUrl = "https://cbs.local"
        });
    }

    [Fact]
    public async Task GetNeighborhoodOverlaysAsync_ReturnsOverlaysOnSuccess()
    {
        var features = new[]
        {
            new
            {
                type = "Feature",
                properties = new { buurtcode = "BU03630000", buurtnaam = "TestBuurt" },
                geometry = new { type = "Polygon", coordinates = new[] { new[] { new[] { 4.9, 52.3 }, new[] { 4.91, 52.31 }, new[] { 4.9, 52.3 } } } }
            }
        };

        var jsonResponse = JsonSerializer.Serialize(new { type = "FeatureCollection", features });
        var handlerMock = CreateHandlerMock(HttpStatusCode.OK, jsonResponse);
        var httpClient = new HttpClient(handlerMock.Object);

        var client = new CbsGeoClient(
            httpClient,
            _cache,
            _statsClientMock.Object,
            _crimeClientMock.Object,
            _options,
            _loggerMock.Object);

        _statsClientMock.Setup(x => x.GetStatsAsync(It.IsAny<ResolvedLocationDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NeighborhoodStatsDto(
                "BU03630000", "Type", 1000, 2000, 300.0, 10.0, 500, 500, 100, 100, 100, 100, 100, 300, 300, 300, 2.5, "High", 30.0, 25.0, 100, 100, 100, 50, 50, 30, 70, 20, 80, 40, 0.8, 100, 500, 0.5, 0.5, 0.5, 0.5, 2.0, DateTimeOffset.UtcNow));

        var result = await client.GetNeighborhoodOverlaysAsync(52.3, 4.9, 52.31, 4.91, MapOverlayMetric.PopulationDensity);

        Assert.Single(result);
        Assert.Equal("BU03630000", result[0].Id);
        Assert.Equal("TestBuurt", result[0].Name);
        Assert.Equal(2000, result[0].MetricValue);
    }

    [Fact]
    public async Task GetNeighborhoodOverlaysAsync_ThrowsOnHttpFailure()
    {
        var handlerMock = CreateHandlerMock(HttpStatusCode.ServiceUnavailable, "{}");
        var httpClient = new HttpClient(handlerMock.Object);

        var client = new CbsGeoClient(
            httpClient,
            _cache,
            _statsClientMock.Object,
            _crimeClientMock.Object,
            _options,
            _loggerMock.Object);

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            client.GetNeighborhoodOverlaysAsync(52.3, 4.9, 52.31, 4.91, MapOverlayMetric.PopulationDensity));
    }

    [Fact]
    public async Task GetNeighborhoodsByMunicipalityAsync_EscapesSpecialCharacters()
    {
        var municipalityName = "Test < & >";
        var features = new[]
        {
            new
            {
                type = "Feature",
                properties = new { buurtcode = "BU1234", buurtnaam = "TestBuurt" }
            }
        };

        var jsonResponse = JsonSerializer.Serialize(new { type = "FeatureCollection", features });

        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock
           .Protected()
           .Setup<Task<HttpResponseMessage>>(
              "SendAsync",
              ItExpr.Is<HttpRequestMessage>(req =>
                  req.RequestUri != null &&
                  // Verify that XML special characters are escaped (e.g., < becomes &lt;)
                  // BEFORE being URL encoded.
                  // If escaped: "Test <" -> "Test &lt;" -> URL Encoded -> "Test%20%26lt%3B"
                  // Unescape -> "Test &lt;"
                  // If NOT escaped: "Test <" -> "Test <" -> URL Encoded -> "Test%20%3C"
                  // Unescape -> "Test <"
                  Uri.UnescapeDataString(req.RequestUri.Query).Contains("&lt;") &&
                  Uri.UnescapeDataString(req.RequestUri.Query).Contains("&gt;") &&
                  Uri.UnescapeDataString(req.RequestUri.Query).Contains("&amp;")
              ),
              ItExpr.IsAny<CancellationToken>()
           )
           .ReturnsAsync(new HttpResponseMessage()
           {
               StatusCode = HttpStatusCode.OK,
               Content = new StringContent(jsonResponse, System.Text.Encoding.UTF8, "application/json"),
           });

        var httpClient = new HttpClient(handlerMock.Object);

        var client = new CbsGeoClient(
            httpClient,
            _cache,
            _statsClientMock.Object,
            _crimeClientMock.Object,
            _options,
            _loggerMock.Object);

        await client.GetNeighborhoodsByMunicipalityAsync(municipalityName);

        // Verify the mock was called
        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Fact]
    public async Task GetNeighborhoodsByMunicipalityAsync_ReturnsEmpty_OnNullOrWhitespace()
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        var httpClient = new HttpClient(handlerMock.Object);

        var client = new CbsGeoClient(
            httpClient,
            _cache,
            _statsClientMock.Object,
            _crimeClientMock.Object,
            _options,
            _loggerMock.Object);

        var result1 = await client.GetNeighborhoodsByMunicipalityAsync(null!);
        var result2 = await client.GetNeighborhoodsByMunicipalityAsync("");
        var result3 = await client.GetNeighborhoodsByMunicipalityAsync("   ");

        Assert.Empty(result1);
        Assert.Empty(result2);
        Assert.Empty(result3);

        // Ensure no requests were made
        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Never(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Fact]
    public async Task GetNeighborhoodsByMunicipalityAsync_ReturnsNeighborhoodsOnSuccess()
    {
        var features = new[]
        {
            new
            {
                type = "Feature",
                properties = new { buurtcode = "BU03630101", buurtnaam = "Burgwallen-Oude Zijde" }
            }
        };

        var jsonResponse = JsonSerializer.Serialize(new { type = "FeatureCollection", features });
        var handlerMock = CreateHandlerMock(HttpStatusCode.OK, jsonResponse);
        var httpClient = new HttpClient(handlerMock.Object);

        var client = new CbsGeoClient(
            httpClient,
            _cache,
            _statsClientMock.Object,
            _crimeClientMock.Object,
            _options,
            _loggerMock.Object);

        var result = await client.GetNeighborhoodsByMunicipalityAsync("Amsterdam");

        Assert.Single(result);
        Assert.Equal("BU03630101", result[0].Code);
        Assert.Equal("Burgwallen-Oude Zijde", result[0].Name);
    }

    [Fact]
    public async Task GetNeighborhoodsByMunicipalityAsync_ReturnsEmpty_OnHttpFailure()
    {
        var handlerMock = CreateHandlerMock(HttpStatusCode.InternalServerError, "{}");
        var httpClient = new HttpClient(handlerMock.Object);

        var client = new CbsGeoClient(
            httpClient,
            _cache,
            _statsClientMock.Object,
            _crimeClientMock.Object,
            _options,
            _loggerMock.Object);

        var result = await client.GetNeighborhoodsByMunicipalityAsync("Amsterdam");

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetNeighborhoodsByMunicipalityAsync_ReturnsEmpty_OnMalformedJson()
    {
        var jsonResponse = "{\"type\": \"FeatureCollection\", \"features\": \"not-an-array\"}";
        var handlerMock = CreateHandlerMock(HttpStatusCode.OK, jsonResponse);
        var httpClient = new HttpClient(handlerMock.Object);

        var client = new CbsGeoClient(
            httpClient,
            _cache,
            _statsClientMock.Object,
            _crimeClientMock.Object,
            _options,
            _loggerMock.Object);

        var result = await client.GetNeighborhoodsByMunicipalityAsync("Amsterdam");

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetNeighborhoodsByMunicipalityAsync_HandlesMissingProperties()
    {
        var features = new object[]
        {
            new
            {
                type = "Feature",
            },
            new
            {
                type = "Feature",
                properties = new { buurtnaam = "NameOnly" }
            }
        };

        var jsonResponse = JsonSerializer.Serialize(new { type = "FeatureCollection", features });
        var handlerMock = CreateHandlerMock(HttpStatusCode.OK, jsonResponse);
        var httpClient = new HttpClient(handlerMock.Object);

        var client = new CbsGeoClient(
            httpClient,
            _cache,
            _statsClientMock.Object,
            _crimeClientMock.Object,
            _options,
            _loggerMock.Object);

        var result = await client.GetNeighborhoodsByMunicipalityAsync("Amsterdam");

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllMunicipalitiesAsync_ReturnsMunicipalitiesOnSuccess()
    {
        var features = new[]
        {
            new
            {
                type = "Feature",
                properties = new { gemeentenaam = "Amsterdam" }
            },
            new
            {
                type = "Feature",
                properties = new { gemeentenaam = "Rotterdam" }
            }
        };

        var jsonResponse = JsonSerializer.Serialize(new { type = "FeatureCollection", features });
        var handlerMock = CreateHandlerMock(HttpStatusCode.OK, jsonResponse);
        var httpClient = new HttpClient(handlerMock.Object);

        var client = new CbsGeoClient(
            httpClient,
            _cache,
            _statsClientMock.Object,
            _crimeClientMock.Object,
            _options,
            _loggerMock.Object);

        var result = await client.GetAllMunicipalitiesAsync();

        Assert.Equal(2, result.Count);
        Assert.Contains("Amsterdam", result);
        Assert.Contains("Rotterdam", result);
    }

    [Fact]
    public async Task GetAllMunicipalitiesAsync_ThrowsOnHttpFailure()
    {
        var handlerMock = CreateHandlerMock(HttpStatusCode.InternalServerError, "{}");
        var httpClient = new HttpClient(handlerMock.Object);

        var client = new CbsGeoClient(
            httpClient,
            _cache,
            _statsClientMock.Object,
            _crimeClientMock.Object,
            _options,
            _loggerMock.Object);

        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAllMunicipalitiesAsync());
    }

    [Fact]
    public async Task GetAllMunicipalitiesAsync_HandlesMalformedJson()
    {
        var jsonResponse = "{\"type\": \"FeatureCollection\", \"features\": \"not-an-array\"}";
        var handlerMock = CreateHandlerMock(HttpStatusCode.OK, jsonResponse);
        var httpClient = new HttpClient(handlerMock.Object);

        var client = new CbsGeoClient(
            httpClient,
            _cache,
            _statsClientMock.Object,
            _crimeClientMock.Object,
            _options,
            _loggerMock.Object);

        var result = await client.GetAllMunicipalitiesAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllMunicipalitiesAsync_HandlesMissingProperties()
    {
        var features = new object[]
        {
            new { type = "Feature" }, // Missing properties
            new { type = "Feature", properties = new { other = "value" } } // Missing gemeentenaam
        };

        var jsonResponse = JsonSerializer.Serialize(new { type = "FeatureCollection", features });
        var handlerMock = CreateHandlerMock(HttpStatusCode.OK, jsonResponse);
        var httpClient = new HttpClient(handlerMock.Object);

        var client = new CbsGeoClient(
            httpClient,
            _cache,
            _statsClientMock.Object,
            _crimeClientMock.Object,
            _options,
            _loggerMock.Object);

        var result = await client.GetAllMunicipalitiesAsync();

        Assert.Empty(result);
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
               Content = new StringContent(content, System.Text.Encoding.UTF8, "application/json"),
           });
        return handlerMock;
    }
}
