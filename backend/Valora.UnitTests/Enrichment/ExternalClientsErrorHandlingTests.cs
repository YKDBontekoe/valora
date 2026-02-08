using System.Net;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Valora.Application.DTOs;
using Valora.Application.Enrichment;
using Valora.Infrastructure.Enrichment;

namespace Valora.UnitTests.Enrichment;

public class ExternalClientsErrorHandlingTests
{
    [Fact]
    public async Task OverpassClient_OnHttpFailure_ReturnsNullAndLogsWarning()
    {
        var logger = new Mock<ILogger<OverpassAmenityClient>>();
        var client = new OverpassAmenityClient(
            new HttpClient(new StaticResponseHandler(() => new HttpResponseMessage(HttpStatusCode.BadGateway))),
            new MemoryCache(new MemoryCacheOptions()),
            Options.Create(new ContextEnrichmentOptions { OverpassBaseUrl = "https://overpass.local" }),
            logger.Object);

        var result = await client.GetAmenitiesAsync(CreateLocation(), 1000);

        Assert.Null(result);
        VerifyWarningLogged(logger);
    }

    [Fact]
    public async Task CbsClient_OnHttpFailure_ReturnsNullAndLogsWarning()
    {
        var logger = new Mock<ILogger<CbsNeighborhoodStatsClient>>();
        var client = new CbsNeighborhoodStatsClient(
            new HttpClient(new StaticResponseHandler(() => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable))),
            new MemoryCache(new MemoryCacheOptions()),
            Options.Create(new ContextEnrichmentOptions { CbsBaseUrl = "https://cbs.local" }),
            logger.Object);

        var result = await client.GetStatsAsync(CreateLocation());

        Assert.Null(result);
        VerifyWarningLogged(logger);
    }

    private static void VerifyWarningLogged<T>(Mock<ILogger<T>> logger) where T : class
    {
        logger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.AtLeastOnce);
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

    private sealed class StaticResponseHandler(Func<HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        private readonly Func<HttpResponseMessage> _responseFactory = responseFactory;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_responseFactory());
        }
    }
}
