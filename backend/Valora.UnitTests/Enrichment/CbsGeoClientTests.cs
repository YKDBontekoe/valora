using System.Net;
using System.Text;
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
    private readonly Mock<ICbsNeighborhoodStatsClient> _statsClientMock;
    private readonly Mock<ICbsCrimeStatsClient> _crimeClientMock;
    private readonly Mock<ILogger<CbsGeoClient>> _loggerMock;
    private readonly IMemoryCache _cache;
    private readonly IOptions<ContextEnrichmentOptions> _options;

    public CbsGeoClientTests()
    {
        _statsClientMock = new Mock<ICbsNeighborhoodStatsClient>();
        _crimeClientMock = new Mock<ICbsCrimeStatsClient>();
        _loggerMock = new Mock<ILogger<CbsGeoClient>>();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _options = Options.Create(new ContextEnrichmentOptions());
    }

    [Fact]
    public async Task GetNeighborhoodOverlaysAsync_ReturnsParsedOverlays()
    {
        // Arrange
        var feature = new
        {
            type = "Feature",
            properties = new { buurtcode = "BU0001", buurtnaam = "Test Buurt" },
            geometry = new { type = "Polygon", coordinates = new[] { new[] { new[] { 4.0, 52.0 }, new[] { 4.1, 52.0 }, new[] { 4.1, 52.1 }, new[] { 4.0, 52.1 }, new[] { 4.0, 52.0 } } } }
        };

        var jsonResponse = JsonSerializer.Serialize(new { features = new[] { feature } });
        var handlerMock = CreateHandlerMock(HttpStatusCode.OK, jsonResponse);
        var httpClient = new HttpClient(handlerMock.Object);

        _crimeClientMock.Setup(x => x.GetStatsAsync(It.IsAny<ResolvedLocationDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CrimeStatsDto(100, 10, 20, 30, 40, null, DateTimeOffset.UtcNow));

        var client = new CbsGeoClient(httpClient, _cache, _statsClientMock.Object, _crimeClientMock.Object, _options, _loggerMock.Object);

        // Act
        var result = await client.GetNeighborhoodOverlaysAsync(52.0, 4.0, 52.1, 4.1, MapOverlayMetric.CrimeRate);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("BU0001", result[0].Id);
        Assert.Equal("Test Buurt", result[0].Name);
        Assert.Equal(100, result[0].MetricValue);
    }

    [Fact]
    public async Task GetNeighborhoodOverlaysAsync_HandlesMissingProperties()
    {
        // Arrange
        var feature = new
        {
            type = "Feature",
            properties = new { buurtnaam = "No Code" }, // Missing buurtcode
            geometry = new { type = "Point", coordinates = new[] { 4.0, 52.0 } }
        };

        var jsonResponse = JsonSerializer.Serialize(new { features = new[] { feature } });
        var handlerMock = CreateHandlerMock(HttpStatusCode.OK, jsonResponse);
        var httpClient = new HttpClient(handlerMock.Object);
        var client = new CbsGeoClient(httpClient, _cache, _statsClientMock.Object, _crimeClientMock.Object, _options, _loggerMock.Object);

        // Act
        var result = await client.GetNeighborhoodOverlaysAsync(52.0, 4.0, 52.1, 4.1, MapOverlayMetric.CrimeRate);

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
               Content = new StringContent(content, Encoding.UTF8, "application/json"),
           });
        return handlerMock;
    }
}
