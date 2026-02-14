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
        Assert.Contains(handler.RequestedUris, uri => uri.AbsolutePath == "/open_api/stations/S1");
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

    [Fact]
    public async Task GetSnapshotAsync_HandlesPagination_AndSkipsFailedDetails()
    {
        var handler = new RecordingHandler(request =>
        {
            var path = request.RequestUri?.AbsolutePath ?? string.Empty;
            var query = request.RequestUri?.Query ?? string.Empty;

            // Page 1: S1 (fails detail fetch)
            if (path == "/open_api/stations" && query == "?page=1")
            {
                return JsonResponse("""
                {
                  "pagination": { "last_page": 2 },
                  "data": [ { "number": "S1", "location": "Station One" } ]
                }
                """);
            }

            // Page 2: S2 (succeeds)
            if (path == "/open_api/stations" && query == "?page=2")
            {
                return JsonResponse("""
                {
                  "pagination": { "last_page": 2 },
                  "data": [ { "number": "S2", "location": "Station Two" } ]
                }
                """);
            }

            // S1 detail failure (e.g. invalid JSON or 404)
            if (path == "/open_api/stations/S1")
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }

            // S2 detail success
            if (path == "/open_api/stations/S2")
            {
                 return JsonResponse("""
                {
                  "data": {
                    "number": "S2",
                    "location": "Station Two",
                    "geometry": { "coordinates": [4.898, 52.377] }
                  }
                }
                """);
            }

            // Measurement for S2
            if (path == "/open_api/stations/S2/measurements")
            {
                return JsonResponse("""
                {
                  "data": [ { "value": 15.0, "timestamp_measured": "2026-01-01T10:00:00Z" } ]
                }
                """);
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        var client = CreateClient(handler);
        var result = await client.GetSnapshotAsync(CreateLocation());

        Assert.NotNull(result);
        Assert.Equal("S2", result!.StationId);
    }

    [Fact]
    public async Task GetSnapshotAsync_WhenMeasurementHttpFails_ReturnsNull()
    {
        var handler = new RecordingHandler(request =>
        {
            var path = request.RequestUri?.AbsolutePath ?? string.Empty;

            if (path == "/open_api/stations" && request.RequestUri?.Query == "?page=1")
            {
                return JsonResponse("""
                {
                  "pagination": { "last_page": 1 },
                  "data": [ { "number": "S1", "location": "Station One" } ]
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

            if (path == "/open_api/stations/S1/measurements")
            {
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        var client = CreateClient(handler);
        var result = await client.GetSnapshotAsync(CreateLocation());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetSnapshotAsync_WhenStationListHttpFails_ContinuesToNextPage()
    {
        // This test simulates a failure on page 1, but success on page 2.
        var handler = new RecordingHandler(request =>
        {
            var path = request.RequestUri?.AbsolutePath ?? string.Empty;
            var query = request.RequestUri?.Query ?? string.Empty;

            if (path == "/open_api/stations" && query == "?page=1")
            {
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }

            if (path == "/open_api/stations" && query == "?page=2")
            {
                return JsonResponse("""
                {
                  "pagination": { "last_page": 2 },
                  "data": [ { "number": "S2", "location": "Station Two" } ]
                }
                """);
            }

            if (path == "/open_api/stations/S2")
            {
                 return JsonResponse("""
                {
                  "data": {
                    "number": "S2",
                    "location": "Station Two",
                    "geometry": { "coordinates": [4.898, 52.377] }
                  }
                }
                """);
            }

            if (path == "/open_api/stations/S2/measurements")
            {
                return JsonResponse("""
                {
                  "data": [ { "value": 10.0, "timestamp_measured": "2026-01-01T10:00:00Z" } ]
                }
                """);
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        var client = CreateClient(handler);
        var result = await client.GetSnapshotAsync(CreateLocation());

        Assert.NotNull(result);
        Assert.Equal("S2", result!.StationId);
    }

    [Fact]
    public async Task GetSnapshotAsync_WithInvalidTimestamp_ReturnsNullTimestamp()
    {
        var handler = new RecordingHandler(request =>
        {
            var path = request.RequestUri?.AbsolutePath ?? string.Empty;

            if (path == "/open_api/stations" && request.RequestUri?.Query == "?page=1")
            {
                return JsonResponse("""
                {
                  "pagination": { "last_page": 1 },
                  "data": [ { "number": "S1", "location": "Station One" } ]
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

            if (path == "/open_api/stations/S1/measurements")
            {
                return JsonResponse("""
                {
                  "data": [ { "value": 10.0, "timestamp_measured": "INVALID-TIMESTAMP" } ]
                }
                """);
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        var client = CreateClient(handler);
        var result = await client.GetSnapshotAsync(CreateLocation());

        Assert.NotNull(result);
        Assert.Null(result!.MeasuredAtUtc);
    }

    [Fact]
    public async Task GetStationDetailAsync_WithInvalidGeometry_ReturnsNull()
    {
         var handler = new RecordingHandler(request =>
        {
            var path = request.RequestUri?.AbsolutePath ?? string.Empty;

            if (path == "/open_api/stations" && request.RequestUri?.Query == "?page=1")
            {
                return JsonResponse("""
                {
                  "pagination": { "last_page": 1 },
                  "data": [ { "number": "S1", "location": "Station One" } ]
                }
                """);
            }

            if (path == "/open_api/stations/S1")
            {
                 // Invalid geometry: less than 2 coordinates
                 return JsonResponse("""
                {
                  "data": {
                    "number": "S1",
                    "location": "Station One",
                    "geometry": { "coordinates": [4.898] }
                  }
                }
                """);
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        var client = CreateClient(handler);
        var result = await client.GetSnapshotAsync(CreateLocation());

        Assert.Null(result); // Should be null because nearest station lookup fails
    }

    [Fact]
    public async Task FetchAllStationIdsAsync_HandlesNullDataAndEmptyIds()
    {
         var handler = new RecordingHandler(request =>
        {
            var path = request.RequestUri?.AbsolutePath ?? string.Empty;
            var query = request.RequestUri?.Query ?? string.Empty;

            if (path == "/open_api/stations" && query == "?page=1")
            {
                // Page 1: Empty data
                return JsonResponse("""
                {
                  "pagination": { "last_page": 2 },
                  "data": []
                }
                """);
            }

            if (path == "/open_api/stations" && query == "?page=2")
            {
                // Page 2: Station with null/whitespace ID
                return JsonResponse("""
                {
                  "pagination": { "last_page": 2 },
                  "data": [ { "number": "", "location": "Invalid" }, { "number": null, "location": "Invalid" } ]
                }
                """);
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        var client = CreateClient(handler);
        var result = await client.GetSnapshotAsync(CreateLocation());

        Assert.Null(result); // Should be null because no valid stations found
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
