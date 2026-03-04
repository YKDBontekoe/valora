using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Valora.Application.Enrichment;
using Valora.Infrastructure.Enrichment;
using Xunit;

namespace Valora.IntegrationTests.Enrichment;

public class WozValuationServiceTests
{
    private readonly Mock<HttpMessageHandler> _handlerMock;
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly IOptions<ContextEnrichmentOptions> _options;

    public WozValuationServiceTests()
    {
        _handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        _httpClient = new HttpClient(_handlerMock.Object);
        _cache = new MemoryCache(new MemoryCacheOptions());
        _options = Options.Create(new ContextEnrichmentOptions { CbsCacheMinutes = 60 });
    }

    [Fact]
    public async Task GetWozValuationAsync_ShouldReturnValuation_WhenFoundViaSuggestApi()
    {
        // Arrange
        SetupHomeResponse();
        SetupSuggestResponse("Utrecht Teststraat 12A", 987654321);
        SetupWozWaardeResponse(987654321, 450000, new DateTime(2023, 1, 1));

        var service = new WozValuationService(_httpClient, _cache, _options, NullLogger<WozValuationService>.Instance);

        // Act
        var result = await service.GetWozValuationAsync("Teststraat", 12, "A", "Utrecht");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(450000, result.Value);
        Assert.Equal("WOZ-waardeloket", result.Source);

        // Verify caching side effect
        var cacheKey = "woz:Utrecht:Teststraat:12A";
        Assert.True(_cache.TryGetValue(cacheKey, out var cached));
        Assert.Equal(result, cached);
    }

    [Fact]
    public async Task GetWozValuationAsync_ShouldUseProvidedId_WhenAvailable()
    {
        // Arrange
        SetupHomeResponse();
        // Notice we don't setup Suggest response because we provide the ID directly
        SetupWozWaardeResponse(123456, 300000, new DateTime(2022, 1, 1));

        var service = new WozValuationService(_httpClient, _cache, _options, NullLogger<WozValuationService>.Instance);

        // Act
        var result = await service.GetWozValuationAsync("Otherstraat", 5, null, "Amsterdam", "123456");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(300000, result.Value);
    }

    [Fact]
    public async Task GetWozValuationAsync_ShouldReturnNull_WhenSuggestFails()
    {
        // Arrange
        SetupHomeResponse();

        // Empty docs
        var responseMsg = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new { docs = Array.Empty<object>() })
        };

        _handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.ToString().Contains("/suggest")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(responseMsg);

        var service = new WozValuationService(_httpClient, _cache, _options, NullLogger<WozValuationService>.Instance);

        // Act
        var result = await service.GetWozValuationAsync("Nostraat", 99, null, "Nowhere");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetWozValuationAsync_ShouldReturnHighestValuation()
    {
        // Arrange
        SetupHomeResponse();

        // Multiple valuations response
        var wozResponse = new
        {
            wozWaarden = new[]
            {
                new { peildatum = "2021-01-01", vastgesteldeWaarde = 200000 },
                new { peildatum = "2023-01-01", vastgesteldeWaarde = 250000 }, // highest
                new { peildatum = "2022-01-01", vastgesteldeWaarde = 220000 }
            }
        };

        var responseMsg = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(wozResponse)
        };

        _handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.ToString().Contains("/wozwaarde/nummeraanduiding/111")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(responseMsg);

        var service = new WozValuationService(_httpClient, _cache, _options, NullLogger<WozValuationService>.Instance);

        // Act
        var result = await service.GetWozValuationAsync("Highstraat", 1, null, "City", "111");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(250000, result.Value);
    }

    private void SetupHomeResponse()
    {
        _handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.ToString() == "https://www.wozwaardeloket.nl/"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));
    }

    private void SetupSuggestResponse(string queryContains, long id)
    {
        var responseObj = new
        {
            docs = new[]
            {
                new { wozObjectNummer = 123, adresseerbaarObjectId = id }
            }
        };

        _handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.ToString().Contains("/suggest")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(responseObj)
            });
    }

    private void SetupWozWaardeResponse(long id, int value, DateTime date)
    {
        var responseObj = new
        {
            wozWaarden = new[]
            {
                new { peildatum = date.ToString("yyyy-MM-dd"), vastgesteldeWaarde = value }
            }
        };

        _handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.ToString().Contains($"/wozwaarde/nummeraanduiding/{id}")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(responseObj)
            });
    }
}
