using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Application.Enrichment;
using Valora.Infrastructure.Enrichment;
using Xunit;

namespace Valora.IntegrationTests.Enrichment;

public class CbsNeighborhoodStatsClientTests
{
    private readonly Mock<HttpMessageHandler> _handlerMock;
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly IOptions<ContextEnrichmentOptions> _options;

    public CbsNeighborhoodStatsClientTests()
    {
        _handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        _httpClient = new HttpClient(_handlerMock.Object);
        _cache = new MemoryCache(new MemoryCacheOptions());
        _options = Options.Create(new ContextEnrichmentOptions { CbsBaseUrl = "https://datasets.cbs.nl/odata/v1", CbsCacheMinutes = 60 });
    }

    [Fact]
    public async Task GetStatsAsync_ShouldReturnStats_WhenDataFound()
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
                    SoortRegio_2 = "Buurt",
                    AantalInwoners_5 = 1500,
                    Bevolkingsdichtheid_34 = 4000,
                    GemiddeldeWOZWaardeVanWoningen_36 = 350.0,
                    HuishoudensMetEenLaagInkomen_87 = 12.5,
                    Mannen_6 = 700,
                    Vrouwen_7 = 800,
                    k_0Tot15Jaar_8 = 300,
                    k_15Tot25Jaar_9 = 200,
                    k_25Tot45Jaar_10 = 400,
                    k_45Tot65Jaar_11 = 400,
                    k_65JaarOfOuder_12 = 200,
                    Eenpersoonshuishoudens_30 = 300,
                    HuishoudensZonderKinderen_31 = 200,
                    HuishoudensMetKinderen_32 = 250,
                    GemiddeldeHuishoudensgrootte_33 = 2.1,
                    MateVanStedelijkheid_125 = "1",
                    GemiddeldInkomenPerInkomensontvanger_80 = 35.5,
                    GemiddeldInkomenPerInwoner_81 = 25.0,
                    BasisonderwijsVmboMbo1_70 = 100,
                    HavoVwoMbo24_71 = 150,
                    HboWo_72 = 250,
                    Koopwoningen_41 = 60,
                    HuurwoningenTotaal_42 = 40,
                    InBezitWoningcorporatie_43 = 30,
                    InBezitOverigeVerhuurders_44 = 10,
                    BouwjaarVoor2000_46 = 70,
                    BouwjaarVanaf2000_47 = 30,
                    PercentageMeergezinswoning_38 = 25,
                    PersonenautoSPerHuishouden_112 = 1.2,
                    PersonenautoSNaarOppervlakte_113 = 3000,
                    PersonenautoSTotaal_109 = 900,
                    AfstandTotHuisartsenpraktijk_115 = 0.5,
                    AfstandTotGroteSupermarkt_116 = 0.8,
                    AfstandTotKinderdagverblijf_117 = 0.4,
                    AfstandTotSchool_118 = 0.6,
                    ScholenBinnen3Km_119 = 5.0
                }
            }
        };

        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = null };
        var jsonContent = JsonSerializer.Serialize(cbsResponse, jsonOptions);

        _handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.ToString().Contains("/85618NED/TypedDataSet") && r.RequestUri!.ToString().Contains("BU000100")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json")
            });

        var client = new CbsNeighborhoodStatsClient(_httpClient, _cache, _options, NullLogger<CbsNeighborhoodStatsClient>.Instance);

        // Act
        var result = await client.GetStatsAsync(location);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("BU000100", result.RegionCode);
        Assert.Equal("Buurt", result.RegionType);
        Assert.Equal(1500, result.Residents);
        Assert.Equal(4000, result.PopulationDensity);
        Assert.Equal(350.0, result.AverageWozValueKeur);
        Assert.Equal(12.5, result.LowIncomeHouseholdsPercent);
        Assert.Equal(700, result.Men);
        Assert.Equal(800, result.Women);
        Assert.Equal(60, result.PercentageOwnerOccupied);
        Assert.Equal(0.8, result.DistanceToSupermarket);

        // Verify caching side effect
        var cacheKey = "cbs:BU000100  ";
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

        var client = new CbsNeighborhoodStatsClient(_httpClient, _cache, _options, NullLogger<CbsNeighborhoodStatsClient>.Instance);

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
                    SoortRegio_2 = "Wijk",
                    AantalInwoners_5 = 5000,
                    Bevolkingsdichtheid_34 = 3000,
                    GemiddeldeWOZWaardeVanWoningen_36 = 400.0,
                    HuishoudensMetEenLaagInkomen_87 = 10.0
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

        var client = new CbsNeighborhoodStatsClient(_httpClient, _cache, _options, NullLogger<CbsNeighborhoodStatsClient>.Instance);

        // Act
        var result = await client.GetStatsAsync(location);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("WK0001", result.RegionCode);
        Assert.Equal("Wijk", result.RegionType);
        Assert.Equal(5000, result.Residents);
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

        var client = new CbsNeighborhoodStatsClient(_httpClient, _cache, _options, NullLogger<CbsNeighborhoodStatsClient>.Instance);

        // Act
        var result = await client.GetStatsAsync(location);

        // Assert
        Assert.Null(result);

        // Verify caching side effect
        var cacheKey = "cbs:BU000100  ";
        Assert.True(_cache.TryGetValue(cacheKey, out var cached));
        Assert.Null(cached);
    }
}
