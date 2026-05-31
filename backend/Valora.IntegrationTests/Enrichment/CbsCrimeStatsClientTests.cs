using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Valora.Application.DTOs;
using Valora.Application.Enrichment;
using Valora.Infrastructure.Enrichment;
using Xunit;

namespace Valora.IntegrationTests.Enrichment;

public class CbsCrimeStatsClientTests
{
    private readonly Mock<HttpMessageHandler> _handlerMock;
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly IOptions<ContextEnrichmentOptions> _options;

    public CbsCrimeStatsClientTests()
    {
        _handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        _httpClient = new HttpClient(_handlerMock.Object);
        _cache = new MemoryCache(new MemoryCacheOptions());
        _options = Options.Create(new ContextEnrichmentOptions { CbsBaseUrl = "https://datasets.cbs.nl/odata/v1", CbsCacheMinutes = 60 });
    }

    [Fact]
    public async Task GetStatsAsync_ShouldReturnStatsAndCalculateRates_WhenDataFound()
    {
        // Arrange
        var location = new ResolvedLocationDto(
            Query: "Teststraat 1",
            DisplayAddress: "Teststraat 1, Teststad",
            Latitude: 52.0,
            Longitude: 5.0,
            RdX: null,
            RdY: null,
            MunicipalityCode: "GM0001",
            MunicipalityName: "Teststad",
            DistrictCode: "WK0001",
            DistrictName: "Testwijk",
            NeighborhoodCode: "BU000100",
            NeighborhoodName: "Testbuurt",
            PostalCode: "1234AB"
        );

        var cbsResponse = new
        {
            value = new[]
            {
                new
                {
                    WijkenEnBuurten = "BU000100  ",
                    AantalInwoners_5 = 2000,
                    TotaalDiefstalUitWoningSchuurED_106 = 10,
                    VernielingMisdrijfTegenOpenbareOrde_107 = 5,
                    GeweldsEnSeksueleMisdrijven_108 = 2
                }
            }
        };

        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = null };
        var jsonContent = JsonSerializer.Serialize(cbsResponse, jsonOptions);

        _handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.ToString().Contains("/83765NED/TypedDataSet") && r.RequestUri!.ToString().Contains("BU000100")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json")
            });

        var client = new CbsCrimeStatsClient(_httpClient, _cache, _options, NullLogger<CbsCrimeStatsClient>.Instance);

        // Act
        var result = await client.GetStatsAsync(location);

        // Assert
        Assert.NotNull(result);

        // Calculations:
        // residents: 2000
        // theft: 10 -> rate per 1000 = (10 * 1000 / 2000) = 5
        // vandalism: 5 -> rate per 1000 = (5 * 1000 / 2000) = 3 (rounded up)
        // violent: 2 -> rate per 1000 = (2 * 1000 / 2000) = 1
        Assert.Equal(5, result.BurglaryPer1000);
        Assert.Equal(5, result.TheftPer1000);
        Assert.Equal(3, result.VandalismPer1000);
        Assert.Equal(1, result.ViolentCrimePer1000);
        Assert.Equal(9, result.TotalCrimesPer1000); // 5 + 3 + 1 = 9

        // Verify caching side effect
        var cacheKey = "cbs-crime:BU000100  ";
        Assert.True(_cache.TryGetValue(cacheKey, out var cached));
        Assert.Equal(result, cached);
    }

    [Fact]
    public async Task GetStatsAsync_ShouldReturnNull_WhenEmptyCandidates()
    {
        // Arrange
        var location = new ResolvedLocationDto(
            Query: "Teststraat 1",
            DisplayAddress: "Teststraat 1, Teststad",
            Latitude: 52.0,
            Longitude: 5.0,
            RdX: null,
            RdY: null,
            MunicipalityCode: null,
            MunicipalityName: null,
            DistrictCode: null,
            DistrictName: null,
            NeighborhoodCode: null,
            NeighborhoodName: null,
            PostalCode: "1234AB"
        );

        var client = new CbsCrimeStatsClient(_httpClient, _cache, _options, NullLogger<CbsCrimeStatsClient>.Instance);

        // Act
        var result = await client.GetStatsAsync(location);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetStatsAsync_ShouldFallbackToNextCandidate_WhenFirstFails()
    {
        // Arrange
        var location = new ResolvedLocationDto(
            Query: "Teststraat 1",
            DisplayAddress: "Teststraat 1, Teststad",
            Latitude: 52.0,
            Longitude: 5.0,
            RdX: null,
            RdY: null,
            MunicipalityCode: "GM0001",
            MunicipalityName: "Teststad",
            DistrictCode: "WK0001",
            DistrictName: "Testwijk",
            NeighborhoodCode: "BU000100",
            NeighborhoodName: "Testbuurt",
            PostalCode: "1234AB"
        );

        // Setup Neighborhood response to return empty array
        var emptyResponse = new { value = Array.Empty<object>() };
        var emptyContent = JsonSerializer.Serialize(emptyResponse);

        _handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.ToString().Contains("BU000100")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(emptyContent, System.Text.Encoding.UTF8, "application/json")
            });

        // Setup District response to return data
        var districtResponse = new
        {
            value = new[]
            {
                new
                {
                    WijkenEnBuurten = "WK0001    ",
                    AantalInwoners_5 = 1000,
                    TotaalDiefstalUitWoningSchuurED_106 = 2,
                    VernielingMisdrijfTegenOpenbareOrde_107 = 2,
                    GeweldsEnSeksueleMisdrijven_108 = 0
                }
            }
        };
        var districtContent = JsonSerializer.Serialize(districtResponse);

        _handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.ToString().Contains("WK0001")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(districtContent, System.Text.Encoding.UTF8, "application/json")
            });

        var client = new CbsCrimeStatsClient(_httpClient, _cache, _options, NullLogger<CbsCrimeStatsClient>.Instance);

        // Act
        var result = await client.GetStatsAsync(location);

        // Assert
        Assert.NotNull(result);

        // Calculations:
        // residents: 1000
        // theft: 2 -> rate per 1000 = (2 * 1000 / 1000) = 2
        // vandalism: 2 -> rate per 1000 = (2 * 1000 / 1000) = 2
        // violent: 0 -> rate per 1000 = (0 * 1000 / 1000) = 0
        Assert.Equal(2, result.BurglaryPer1000);
        Assert.Equal(2, result.TheftPer1000);
        Assert.Equal(2, result.VandalismPer1000);
        Assert.Equal(0, result.ViolentCrimePer1000);
        Assert.Equal(4, result.TotalCrimesPer1000); // 2 + 2 + 0 = 4
    }

    [Fact]
    public async Task GetStatsAsync_ShouldCacheNullResult_WhenNotFound()
    {
        // Arrange
        var location = new ResolvedLocationDto(
            Query: "Teststraat 1",
            DisplayAddress: "Teststraat 1, Teststad",
            Latitude: 52.0,
            Longitude: 5.0,
            RdX: null,
            RdY: null,
            MunicipalityCode: null,
            MunicipalityName: null,
            DistrictCode: null,
            DistrictName: null,
            NeighborhoodCode: "BU000100",
            NeighborhoodName: "Testbuurt",
            PostalCode: "1234AB"
        );

        var emptyResponse = new { value = Array.Empty<object>() };
        var emptyContent = JsonSerializer.Serialize(emptyResponse);

        _handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.ToString().Contains("BU000100")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(emptyContent, System.Text.Encoding.UTF8, "application/json")
            });

        var client = new CbsCrimeStatsClient(_httpClient, _cache, _options, NullLogger<CbsCrimeStatsClient>.Instance);

        // Act
        var result = await client.GetStatsAsync(location);

        // Assert
        Assert.Null(result);

        // Verify caching side effect
        var cacheKey = "cbs-crime:BU000100  ";
        Assert.True(_cache.TryGetValue(cacheKey, out var cached));
        Assert.Null(cached);
    }
}
