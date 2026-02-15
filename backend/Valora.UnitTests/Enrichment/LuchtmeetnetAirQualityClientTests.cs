using System.Net;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Valora.Application.DTOs;
using Valora.Application.Enrichment;
using Valora.Application.Common.Interfaces;
using Valora.Infrastructure.Enrichment;
using Valora.Domain.Entities;
using Xunit;

namespace Valora.UnitTests.Enrichment;

public class LuchtmeetnetAirQualityClientTests
{
    [Fact]
    public async Task GetSnapshotAsync_FetchesStationDetails_ToFindNearest()
    {
        var handler = new RecordingHandler(request =>
        {
            var path = request.RequestUri?.AbsolutePath ?? string.Empty;
            var query = request.RequestUri?.Query ?? string.Empty;

            if (path == "/open_api/stations" && query == "?page=1")
            {
                return JsonResponse("""
                {
                  "pagination": { "last_page": 1 },
                  "data": [
                    {
                      "number": "S1",
                      "location": "Station One"
                    }
                  ]
                }
                """);
            }

            if (path == "/open_api/stations/S1")
            {
                 return JsonResponse("""
                {
                  "data": {
                    "number": "S1",
                    "location": "Station One",
                    "geometry": { "coordinates": [4.898, 52.377] }
                  }
                }
                """);
            }

            if (path.Contains("/measurements"))
            {
                 return JsonResponse("""
                {
                  "data": [
                    { "formula": "PM25", "value": 12.3, "timestamp_measured": "2024-01-01T12:00:00Z" }
                  ]
                }
                """);
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        var client = CreateClient(handler);
        var result = await client.GetSnapshotAsync(CreateLocation());

        Assert.NotNull(result);
        Assert.Equal("S1", result.StationId);
        Assert.Equal(12.3, result.Pm25);
    }

    [Fact]
    public async Task GetSnapshotAsync_WhenMemCacheMiss_ChecksDbCache()
    {
        var location = CreateLocation();
        var mockDbCache = new Mock<IContextCacheRepository>();
        var dbSnapshot = new AirQualitySnapshot { StationId = "S1", Pm25 = 15.0, ExpiresAtUtc = DateTimeOffset.UtcNow.AddHours(1) };

        mockDbCache.Setup(x => x.GetAirQualitySnapshotAsync("S1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(dbSnapshot);

        var handler = new RecordingHandler(request => {
            if (request.RequestUri?.AbsolutePath == "/open_api/stations" && request.RequestUri.Query == "?page=1") {
                return JsonResponse("""{"pagination": {"last_page": 1}, "data": [{"number": "S1", "location": "Station One"}]}""");
            }
            if (request.RequestUri?.AbsolutePath == "/open_api/stations/S1") {
                return JsonResponse("""{"data": {"number": "S1", "location": "Station One", "geometry": {"coordinates": [4.898, 52.377]}}}""");
            }
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        var client = new LuchtmeetnetAirQualityClient(
            new HttpClient(handler) { BaseAddress = new Uri("https://lucht.local") },
            new MemoryCache(new MemoryCacheOptions()),
            mockDbCache.Object,
            Options.Create(new ContextEnrichmentOptions { LuchtmeetnetBaseUrl = "https://lucht.local", AirQualityCacheMinutes = 1 }),
            new Mock<ILogger<LuchtmeetnetAirQualityClient>>().Object);

        var result = await client.GetSnapshotAsync(location);

        Assert.NotNull(result);
        Assert.Equal(15.0, result.Pm25);
        mockDbCache.Verify(x => x.GetAirQualitySnapshotAsync("S1", It.IsAny<CancellationToken>()), Times.Once);
    }

    private static LuchtmeetnetAirQualityClient CreateClient(RecordingHandler handler)
    {
        return new LuchtmeetnetAirQualityClient(
            new HttpClient(handler) { BaseAddress = new Uri("https://lucht.local") },
            new MemoryCache(new MemoryCacheOptions()),
            new Mock<IContextCacheRepository>().Object,
            Options.Create(new ContextEnrichmentOptions
            {
                LuchtmeetnetBaseUrl = "https://lucht.local",
                AirQualityCacheMinutes = 1
            }),
            new Mock<ILogger<LuchtmeetnetAirQualityClient>>().Object);
    }

    private static ResolvedLocationDto CreateLocation()
    {
        return new ResolvedLocationDto(
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

    private static HttpResponseMessage JsonResponse(string json)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder = responder;
        public List<Uri> RequestedUris { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri is not null)
            {
                RequestedUris.Add(request.RequestUri);
            }

            return Task.FromResult(_responder(request));
        }
    }
}
