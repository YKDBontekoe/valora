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
        // Arrange
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

        // Act
        var result = await client.GetNeighborhoodOverlaysAsync(52.3, 4.9, 52.31, 4.91, MapOverlayMetric.PopulationDensity);

        // Assert
        Assert.Single(result);
        Assert.Equal("BU03630000", result[0].Id);
        Assert.Equal("TestBuurt", result[0].Name);
        Assert.Equal(2000, result[0].MetricValue);
        Assert.Equal("2000 / km²", result[0].DisplayValue);
    }

    [Fact]
    public async Task GetNeighborhoodOverlaysAsync_ThrowsOnHttpFailure()
    {
        // Arrange
        var handlerMock = CreateHandlerMock(HttpStatusCode.ServiceUnavailable, "{}");
        var httpClient = new HttpClient(handlerMock.Object);

        var client = new CbsGeoClient(
            httpClient,
            _cache,
            _statsClientMock.Object,
            _crimeClientMock.Object,
            _options,
            _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            client.GetNeighborhoodOverlaysAsync(52.3, 4.9, 52.31, 4.91, MapOverlayMetric.PopulationDensity));
    }

    [Fact]
    public async Task GetNeighborhoodOverlaysAsync_CachesResult()
    {
        // Arrange
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

        // Act
        var result1 = await client.GetNeighborhoodOverlaysAsync(52.3, 4.9, 52.31, 4.91, MapOverlayMetric.PopulationDensity);
        var result2 = await client.GetNeighborhoodOverlaysAsync(52.3, 4.9, 52.31, 4.91, MapOverlayMetric.PopulationDensity);

        // Assert
        Assert.Single(result1);
        Assert.Same(result1, result2);

        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task GetNeighborhoodOverlaysAsync_HandlesMissingStats()
    {
        // Arrange
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
            .ReturnsAsync((NeighborhoodStatsDto?)null);

        // Act
        var result = await client.GetNeighborhoodOverlaysAsync(52.3, 4.9, 52.31, 4.91, MapOverlayMetric.PopulationDensity);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetNeighborhoodOverlaysAsync_HandlesCrimeMetric()
    {
        // Arrange
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

        _crimeClientMock.Setup(x => x.GetStatsAsync(It.IsAny<ResolvedLocationDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CrimeStatsDto(
                50, 10, 5, 2, 1, 32.0, DateTimeOffset.UtcNow));

        // Act
        var result = await client.GetNeighborhoodOverlaysAsync(52.3, 4.9, 52.31, 4.91, MapOverlayMetric.CrimeRate);

        // Assert
        Assert.Single(result);
        Assert.Equal(50.0, result[0].MetricValue);
        Assert.Equal("50 / 1000", result[0].DisplayValue);
    }

    [Fact]
    public async Task GetNeighborhoodsByMunicipalityAsync_ReturnsNeighborhoodsOnSuccess()
    {
        // Arrange
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

        // Act
        var result = await client.GetNeighborhoodsByMunicipalityAsync("Amsterdam");

        // Assert
        Assert.Single(result);
        Assert.Equal("BU03630101", result[0].Code);
        Assert.Equal("Burgwallen-Oude Zijde", result[0].Name);
    }

    [Fact]
    public async Task GetNeighborhoodsByMunicipalityAsync_HandlesMissingProperties()
    {
        // Arrange
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

        // Act
        var result = await client.GetNeighborhoodsByMunicipalityAsync("Amsterdam");

        // Assert
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
