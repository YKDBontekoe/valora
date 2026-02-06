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
        var price = 500000;

        // Mock API Response (FundaApiClient uses POST to /api/topposition/v2/search)
        // Note: We force Dutch format (dots for thousands) because FundaValueParser expects it.
        // Hardcoded to ensure test stability across different environments (CI/Local)
        var priceString = "500.000";
        var apiResponseJson = $@"
        {{
            ""listings"": [
                {{
                    ""globalId"": {fundaId},
                    ""price"": ""€ {priceString} k.k."",
                    ""isSinglePrice"": true,
                    ""listingUrl"": ""/koop/amsterdam/huis-{fundaId}-test-street-1/"",
                    ""image"": {{
                        ""default"": ""https://cloud.funda.nl/test.jpg""
                    }},
                    ""address"": {{
                        ""listingAddress"": ""{address}"",
                        ""city"": ""{city}""
                    }},
                    ""isProject"": false
                }}
            ]
        }}";

        _server.Given(Request.Create().UsingPost().WithPath("/api/topposition/v2/search"))
               .RespondWith(Response.Create()
                   .WithStatusCode(200)
                   .WithHeader("Content-Type", "application/json")
                   .WithBody(apiResponseJson));

        // Act
        // Create a custom scope where we replace the HttpClient logic
        using var clientFactory = Factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                // Intercept HttpClient for FundaApiClient
                services.AddHttpClient<FundaApiClient>()
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
        
        // API-only scraping provides limited details compared to HTML scraping
        Assert.Null(listing.LivingAreaM2);
        Assert.Null(listing.PlotAreaM2);
        Assert.Null(listing.Bedrooms);
        
        Assert.Equal("Woonhuis", listing.PropertyType); // Default for non-project
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
