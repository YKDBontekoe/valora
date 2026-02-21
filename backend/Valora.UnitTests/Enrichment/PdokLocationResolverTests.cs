using System.Net;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Valora.Application.Enrichment;
using Valora.Infrastructure.Enrichment;

namespace Valora.UnitTests.Enrichment;

public class PdokLocationResolverTests
{
    [Fact]
    public async Task ResolveAsync_OnHttpFailure_DoesNotLogInputValue()
    {
        var logger = new Mock<ILogger<PdokLocationResolver>>();
        var handler = new StaticResponseHandler(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var resolver = new PdokLocationResolver(
            new HttpClient(handler) { BaseAddress = new Uri("https://pdok.local") },
            new MemoryCache(new MemoryCacheOptions()),
            Options.Create(new ContextEnrichmentOptions { PdokBaseUrl = "https://pdok.local" }),
            logger.Object);

        var input = "Damrak 1 Amsterdam";
        _ = await resolver.ResolveAsync(input);

        logger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => !v.ToString()!.Contains(input, StringComparison.Ordinal)),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task ResolveAsync_NonFundaUrl_UsesUrlHintInsteadOfRawUrlInPdokQuery()
    {
        var logger = new Mock<ILogger<PdokLocationResolver>>();
        var handler = new CapturingResponseHandler(new HttpResponseMessage(HttpStatusCode.BadRequest));
        var resolver = new PdokLocationResolver(
            new HttpClient(handler) { BaseAddress = new Uri("https://pdok.local") },
            new MemoryCache(new MemoryCacheOptions()),
            Options.Create(new ContextEnrichmentOptions { PdokBaseUrl = "https://pdok.local" }),
            logger.Object);

        _ = await resolver.ResolveAsync("https://example.com/properties/damrak-1-amsterdam");

        Assert.NotNull(handler.LastRequestUri);
        var decodedQuery = Uri.UnescapeDataString(handler.LastRequestUri!.Query).Replace('+', ' ');
        Assert.Contains("q=damrak 1 amsterdam", decodedQuery, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("https://example.com", decodedQuery, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ResolveAsync_UrlWithQueryHint_UsesQueryValueInPdokQuery()
    {
        var logger = new Mock<ILogger<PdokLocationResolver>>();
        var handler = new CapturingResponseHandler(new HttpResponseMessage(HttpStatusCode.BadRequest));
        var resolver = new PdokLocationResolver(
            new HttpClient(handler) { BaseAddress = new Uri("https://pdok.local") },
            new MemoryCache(new MemoryCacheOptions()),
            Options.Create(new ContextEnrichmentOptions { PdokBaseUrl = "https://pdok.local" }),
            logger.Object);

        _ = await resolver.ResolveAsync("https://maps.example.com/search?query=Damrak%201%20Amsterdam");

        Assert.NotNull(handler.LastRequestUri);
        var decodedQuery = Uri.UnescapeDataString(handler.LastRequestUri!.Query).Replace('+', ' ');
        Assert.Contains("q=Damrak 1 Amsterdam", decodedQuery, StringComparison.Ordinal);
    }

    private sealed class StaticResponseHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response = response;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_response);
        }
    }

    private sealed class CapturingResponseHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response = response;
        public Uri? LastRequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri;
            return Task.FromResult(_response);
        }
    }
}
