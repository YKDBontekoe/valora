using System.Net;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Valora.Application.DTOs;
using Valora.Application.Enrichment;
using Valora.Infrastructure.Enrichment;
using Xunit;

namespace Valora.UnitTests.Enrichment;

public class ExternalClientsErrorHandlingTests2
{
    [Fact]
    public async Task WozValuationService_OnHttpFailure_ReturnsNullAndLogsWarning()
    {
        var logger = new Mock<ILogger<WozValuationService>>();
        var client = new WozValuationService(
            new HttpClient(new ExceptionResponseHandler(() => new HttpRequestException("Network Error"))),
            new MemoryCache(new MemoryCacheOptions()),
            Options.Create(new ContextEnrichmentOptions()),
            logger.Object);

        var result = await client.GetWozValuationAsync("street", 1, null, "city");
        Assert.Null(result);

        logger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task WozValuationService_OnJsonException_ReturnsNullAndLogsWarning()
    {
        var logger = new Mock<ILogger<WozValuationService>>();
        var client = new WozValuationService(
            new HttpClient(new ExceptionResponseHandler(() => new System.Text.Json.JsonException("Invalid JSON"))),
            new MemoryCache(new MemoryCacheOptions()),
            Options.Create(new ContextEnrichmentOptions()),
            logger.Object);

        var result = await client.GetWozValuationAsync("street", 1, null, "city");
        Assert.Null(result);

        logger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task PdokLocationResolver_OnJsonException_ReturnsNullAndLogsWarning()
    {
        var logger = new Mock<ILogger<PdokLocationResolver>>();
        var client = new PdokLocationResolver(
            new HttpClient(new ExceptionResponseHandler(() => new System.Text.Json.JsonException("Invalid JSON"))),
            new MemoryCache(new MemoryCacheOptions()),
            Options.Create(new ContextEnrichmentOptions { PdokBaseUrl = "http://pdok.local" }),
            logger.Object);

        var result = await client.ResolveAsync("query");
        Assert.Null(result);

        logger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task PdokLocationResolver_OnHttpFailure_ReturnsNullAndLogsWarning()
    {
        var logger = new Mock<ILogger<PdokLocationResolver>>();
        var client = new PdokLocationResolver(
            new HttpClient(new ExceptionResponseHandler(() => new HttpRequestException("Network Error"))),
            new MemoryCache(new MemoryCacheOptions()),
            Options.Create(new ContextEnrichmentOptions { PdokBaseUrl = "http://pdok.local" }),
            logger.Object);

        var result = await client.ResolveAsync("query");
        Assert.Null(result);

        logger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.AtLeastOnce);
    }

    private sealed class ExceptionResponseHandler(Func<Exception> exceptionFactory) : HttpMessageHandler
    {
        private readonly Func<Exception> _exceptionFactory = exceptionFactory;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            throw _exceptionFactory();
        }
    }
}
