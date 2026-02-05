using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Valora.IntegrationTests;

public class RedirectHandler : DelegatingHandler
{
    private readonly string _replacementBase;

    public RedirectHandler(string replacementBase)
    {
        _replacementBase = replacementBase;
        InnerHandler = new HttpClientHandler();
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.RequestUri != null)
        {
            var builder = new UriBuilder(request.RequestUri);
            var wireMockUri = new Uri(_replacementBase);

            builder.Scheme = wireMockUri.Scheme;
            builder.Host = wireMockUri.Host;
            builder.Port = wireMockUri.Port;

            request.RequestUri = builder.Uri;
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
