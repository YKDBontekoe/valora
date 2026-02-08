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

    private sealed class StaticResponseHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response = response;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_response);
        }
    }
}
