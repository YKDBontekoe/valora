using System.Net;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Valora.Application.DTOs;
using Valora.Application.Enrichment;
using Valora.Infrastructure.Enrichment;

namespace Valora.UnitTests.Enrichment;

public class LuchtmeetnetAirQualityClientTests
{
    [Fact]
    public async Task GetSnapshotAsync_UsesStationListCoordinates_WithoutStationDetailRequests()
    {
        var handler = new RecordingHandler(request =>
        {
            var path = request.RequestUri?.AbsolutePath ?? string.Empty;
            var query = request.RequestUri?.Query ?? string.Empty;

            if (path == "/open_api/stations" && query == "?page=1")
            {
                return JsonResponse("""
                {
                  "data": [
                    {
                      "number": "S1",
                      "location": "Station One",
                      "geometry": { "coordinates": [4.898, 52.377] }
                    }
                  ]
                }
                """);
            }

            if (path == "/open_api/stations/S1/measurements")
            {
                return JsonResponse("""
                {
                  "data": [
                    {
                      "value": 11.2,
                      "timestamp_measured": "2026-01-01T10:00:00Z"
                    }
                  ]
                }
                """);
            }

            if (path == "/open_api/stations")
            {
                return JsonResponse("""{ "data": [] }""");
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        var client = CreateClient(handler);

        var result = await client.GetSnapshotAsync(CreateLocation());

        Assert.NotNull(result);
        Assert.Equal("S1", result!.StationId);
        Assert.DoesNotContain(handler.RequestedUris, uri => uri.AbsolutePath == "/open_api/stations/S1");
    }

    [Fact]
    public async Task GetSnapshotAsync_WhenMeasurementPayloadHasNoDataArray_ReturnsNull()
    {
        var handler = new RecordingHandler(request =>
        {
            var path = request.RequestUri?.AbsolutePath ?? string.Empty;
            var query = request.RequestUri?.Query ?? string.Empty;

            if (path == "/open_api/stations" && query == "?page=1")
            {
                return JsonResponse("""
                {
                  "data": [
                    {
                      "number": "S1",
                      "location": "Station One",
                      "geometry": { "coordinates": [4.898, 52.377] }
                    }
                  ]
                }
                """);
            }

            if (path == "/open_api/stations/S1/measurements")
            {
                return JsonResponse("""{ "unexpected": "shape" }""");
            }

            if (path == "/open_api/stations")
            {
                return JsonResponse("""{ "data": [] }""");
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        var client = CreateClient(handler);

        var result = await client.GetSnapshotAsync(CreateLocation());

        Assert.Null(result);
    }

    private static LuchtmeetnetAirQualityClient CreateClient(RecordingHandler handler)
    {
        return new LuchtmeetnetAirQualityClient(
            new HttpClient(handler) { BaseAddress = new Uri("https://lucht.local") },
            new MemoryCache(new MemoryCacheOptions()),
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
