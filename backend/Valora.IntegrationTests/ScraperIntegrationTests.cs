using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Valora.Application.Common.Interfaces;
using Valora.Application.Scraping;
using Valora.Infrastructure.Scraping;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace Valora.IntegrationTests;

public class ScraperIntegrationTests : BaseIntegrationTest, IDisposable
{
    private readonly WireMockServer _server;

    public ScraperIntegrationTests(TestDatabaseFixture fixture) : base(fixture)
    {
        _server = WireMockServer.Start();
    }

    [Fact]
    public async Task ScrapeLimitedAsync_ShouldParseAndSaveListing()
    {
        // Arrange
        var fundaId = "12345678";
        var address = "Test Street 1";
        var city = "Amsterdam";
        var postalCode = "1234 AB";
        var price = 500000;

        var searchUrlPath = "/koop/amsterdam/";
        var detailUrlPath = $"/detail/koop/amsterdam/huis-{fundaId}-test-street-1/";

        // Mock Search Page
        var searchHtml = $@"
            <html>
                <body>
                    <div class=""search-result"">
                        <a href=""{detailUrlPath}"" class=""search-result__header-title-col"">
                            <span class=""search-result__header-title"">Test Listing</span>
                            <span class=""search-result__price"">€ {price:N0} k.k.</span>
                        </a>
                    </div>
                </body>
            </html>";

        _server.Given(Request.Create().UsingGet().WithPath(searchUrlPath))
               .RespondWith(Response.Create().WithStatusCode(200).WithBody(searchHtml));

        // Mock Detail Page
        var detailHtml = $@"
            <html>
                <body>
                    <h1>{address} {postalCode} {city}</h1>
                    <span class=""price"">€ {price:N0} k.k.</span>

                    <dl>
                        <dt>Wonen</dt>
                        <dd>120 m²</dd>
                        <dt>Perceel</dt>
                        <dd>200 m²</dd>
                        <dt>Aantal kamers</dt>
                        <dd>5 kamers (4 slaapkamers)</dd>
                        <dt>Soort woonhuis</dt>
                        <dd>Eengezinswoning</dd>
                        <dt>Status</dt>
                        <dd>Beschikbaar</dd>
                    </dl>

                    <img src=""https://cloud.funda.nl/test.jpg"" alt=""Test Image"">
                </body>
            </html>";

        _server.Given(Request.Create().UsingGet().WithPath(detailUrlPath))
               .RespondWith(Response.Create().WithStatusCode(200).WithBody(detailHtml));

        // Act
        // Create a custom scope where we replace the HttpClient logic
        using var clientFactory = Factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                // Intercept HttpClient for IFundaScraperService
                services.AddHttpClient<IFundaScraperService, FundaScraperService>()
                        .ConfigurePrimaryHttpMessageHandler(() => new RedirectHandler(_server.Urls[0]));
            });
        });

        using var scope = clientFactory.Services.CreateScope();
        var scraperService = scope.ServiceProvider.GetRequiredService<IFundaScraperService>();

        await scraperService.ScrapeLimitedAsync("amsterdam", 1, CancellationToken.None);

        // Assert
        var listingRepository = scope.ServiceProvider.GetRequiredService<IListingRepository>();
        var listing = await listingRepository.GetByFundaIdAsync(fundaId);

        Assert.NotNull(listing);
        Assert.Equal(fundaId, listing.FundaId);
        Assert.Equal(address, listing.Address);
        Assert.Equal(city, listing.City);
        Assert.Equal(price, listing.Price);
        Assert.Equal(120, listing.LivingAreaM2);
        Assert.Equal(200, listing.PlotAreaM2);
        Assert.Equal(4, listing.Bedrooms);
        Assert.Equal("Eengezinswoning", listing.PropertyType);
        Assert.Equal("Beschikbaar", listing.Status);

        var priceHistoryRepository = scope.ServiceProvider.GetRequiredService<IPriceHistoryRepository>();
        var history = await priceHistoryRepository.GetByListingIdAsync(listing.Id);
        Assert.NotEmpty(history);
        Assert.Equal(price, history.First().Price);
    }

    public void Dispose()
    {
        _server.Stop();
        _server.Dispose();
        // BaseIntegrationTest doesn't implement IDisposable, but IAsyncLifetime.
        // But the class definition adds IDisposable.
    }
}

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
