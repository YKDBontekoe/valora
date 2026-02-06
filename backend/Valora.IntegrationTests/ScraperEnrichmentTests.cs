using System.Net;
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

public class ScraperEnrichmentTests : BaseIntegrationTest, IDisposable
{
    private readonly WireMockServer _server;
    private IDisposable? _customFactory;
    private IServiceScope? _customScope;

    public ScraperEnrichmentTests(TestDatabaseFixture fixture) : base(fixture)
    {
        _server = WireMockServer.Start();
    }

    public void Dispose()
    {
        _customScope?.Dispose();
        _customFactory?.Dispose();
        _server.Stop();
        _server.Dispose();
        base.DisposeAsync().GetAwaiter().GetResult();
    }

    private IFundaScraperService GetScraperServiceWithMockedApi()
    {
        // Intercept HttpClient for FundaApiClient
        var clientFactory = Factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddHttpClient<IFundaApiClient, FundaApiClient>()
                        .ConfigurePrimaryHttpMessageHandler(() => new RedirectHandler(_server.Urls[0]));
            });
        });

        _customFactory = clientFactory;
        _customScope = clientFactory.Services.CreateScope();

        return _customScope.ServiceProvider.GetRequiredService<IFundaScraperService>();
    }

    private void SetupSearchResponse(string fundaId, string address, string city = "Amsterdam")
    {
        var apiResponseJson = $@"
        {{
            ""listings"": [
                {{
                    ""globalId"": {fundaId},
                    ""price"": ""€ 500.000 k.k."",
                    ""isSinglePrice"": true,
                    ""listingUrl"": ""/koop/{city.ToLower()}/huis-{fundaId}-test-address/"",
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
    }

    private void SetupSummaryResponse(string fundaId, bool success = true)
    {
        if (!success)
        {
            _server.Given(Request.Create().UsingGet().WithPath(p => p.Contains($"/api/detail-summary/v2/getsummary/{fundaId}")))
                   .RespondWith(Response.Create().WithStatusCode(500));
            return;
        }

        var json = $@"
        {{
            ""identifiers"": {{ ""globalId"": {fundaId} }},
            ""publicationDate"": ""2023-01-01"",
            ""labels"": [{{ ""text"": ""Verkocht"" }}],
            ""address"": {{
                ""postCode"": ""1011AB"",
                ""city"": ""Amsterdam""
            }}
        }}";

        _server.Given(Request.Create().UsingGet().WithPath(p => p.Contains($"/api/detail-summary/v2/getsummary/{fundaId}")))
               .RespondWith(Response.Create()
                   .WithStatusCode(200)
                   .WithHeader("Content-Type", "application/json")
                   .WithBody(json));
    }

    private void SetupContactResponse(string fundaId, bool success = true)
    {
        if (!success)
        {
            _server.Given(Request.Create().UsingGet().WithPath(p => p.Contains($"/api/v3/listings/{fundaId}/contact-details")))
                   .RespondWith(Response.Create().WithStatusCode(404));
            return;
        }

        var json = @"
        {
            ""contactBlockDetails"": [
                {
                    ""id"": 123,
                    ""phoneNumber"": ""020-1234567"",
                    ""logoUrl"": ""https://logo.url"",
                    ""associationCode"": ""NVM"",
                    ""displayName"": ""Best Broker""
                }
            ]
        }";

        _server.Given(Request.Create().UsingGet().WithPath(p => p.Contains($"/api/v3/listings/{fundaId}/contact-details")))
               .RespondWith(Response.Create()
                   .WithStatusCode(200)
                   .WithHeader("Content-Type", "application/json")
                   .WithBody(json));
    }

    private void SetupFiberResponse(string postalCode, bool success = true)
    {
        // Fiber API uses clean postal code
        var cleanPostCode = postalCode.Replace(" ", "").ToUpper();

        if (!success)
        {
            _server.Given(Request.Create().UsingGet().WithPath(p => p.Contains($"/api/v1/{cleanPostCode}")))
                   .RespondWith(Response.Create().WithStatusCode(500));
            return;
        }

        var json = @"
        {
            ""availability"": true
        }";

        _server.Given(Request.Create().UsingGet().WithPath(p => p.Contains($"/api/v1/{cleanPostCode}")))
               .RespondWith(Response.Create()
                   .WithStatusCode(200)
                   .WithHeader("Content-Type", "application/json")
                   .WithBody(json));
    }

    private void SetupHtmlResponse(string fundaId, bool success = true)
    {
        if (!success)
        {
            // Match any listing page URL
            _server.Given(Request.Create().UsingGet().WithPath(p => p.Contains($"/huis-{fundaId}-")))
                   .RespondWith(Response.Create().WithStatusCode(404));
            return;
        }

        // We need to embed the Nuxt JSON in a script tag
        // Constructing a minimal valid Nuxt state for FundaNuxtJsonParser (BFS)
        // It looks for an object with features, media, description
        var nuxtJson = @"
        {
            ""route"": { },
            ""payload"": {
                ""data"": {
                    ""listing"": {
                        ""features"": {
                            ""indeling"": {
                                ""Title"": ""Indeling"",
                                ""KenmerkenList"": [
                                    { ""Label"": ""Aantal kamers"", ""Value"": ""4 kamers"" }
                                ]
                            },
                            ""afmetingen"": {
                                ""Title"": ""Afmetingen"",
                                ""KenmerkenList"": [
                                    { ""Label"": ""Wonen"", ""Value"": ""120 m²"" }
                                ]
                            },
                            ""media"": [],
                            ""description"": { ""content"": ""Great house!"" }
                        },
                        ""media"": { ""items"": [] },
                        ""description"": { ""content"": ""Great house!"" }
                    }
                }
            }
        }";

        var html = $@"
        <html>
            <body>
                <h1>Listing Page</h1>
                <script type=""application/json"">{nuxtJson}</script>
            </body>
        </html>";

        _server.Given(Request.Create().UsingGet().WithPath(p => p.Contains($"/huis-{fundaId}-")))
               .RespondWith(Response.Create()
                   .WithStatusCode(200)
                   .WithHeader("Content-Type", "text/html")
                   .WithBody(html));
    }

    [Fact]
    public async Task Scrape_FullEnrichment_Success()
    {
        // Arrange
        var fundaId = "9001";
        var address = "Rich Street 1";

        SetupSearchResponse(fundaId, address);
        SetupSummaryResponse(fundaId, success: true);
        SetupContactResponse(fundaId, success: true);
        SetupFiberResponse("1011AB", success: true);
        SetupHtmlResponse(fundaId, success: true);

        var scraper = GetScraperServiceWithMockedApi();

        // Act
        await scraper.ScrapeLimitedAsync("amsterdam", 1, CancellationToken.None);

        // Assert
        var listing = await DbContext.Listings.FirstOrDefaultAsync(l => l.FundaId == fundaId);

        Assert.NotNull(listing);
        Assert.Equal("1011AB", listing.PostalCode); // From Summary
        Assert.Equal(4, listing.Bedrooms); // From Nuxt (4 kamers)
        Assert.Equal(120, listing.LivingAreaM2); // From Nuxt (120 m2)
        Assert.Equal("Best Broker", listing.AgentName); // From Contact
        Assert.Equal("020-1234567", listing.BrokerPhone); // From Contact
        Assert.True(listing.FiberAvailable); // From Fiber
    }

    [Fact]
    public async Task Scrape_GracefulDegradation_SummaryFails()
    {
        // Arrange
        var fundaId = "9002";
        var address = "NoSummary St 2";

        SetupSearchResponse(fundaId, address);
        SetupSummaryResponse(fundaId, success: false); // Fails
        // Note: Fiber check relies on PostalCode from Summary. If Summary fails, PostalCode is null (unless parsed from address string, but typical flow relies on Summary).
        // If PostalCode is null, Fiber check is skipped.

        SetupContactResponse(fundaId, success: true);
        SetupHtmlResponse(fundaId, success: true);

        var scraper = GetScraperServiceWithMockedApi();

        // Act
        await scraper.ScrapeLimitedAsync("amsterdam", 1, CancellationToken.None);

        // Assert
        var listing = await DbContext.Listings.FirstOrDefaultAsync(l => l.FundaId == fundaId);

        Assert.NotNull(listing);
        Assert.Null(listing.PostalCode); // Missing because Summary failed
        Assert.Equal(4, listing.Bedrooms); // Nuxt still works
        Assert.Equal("Best Broker", listing.AgentName); // Contact still works
    }

    [Fact]
    public async Task Scrape_GracefulDegradation_NuxtFails()
    {
        // Arrange
        var fundaId = "9003";
        var address = "NoNuxt St 3";

        SetupSearchResponse(fundaId, address);
        SetupSummaryResponse(fundaId, success: true);
        SetupContactResponse(fundaId, success: true);
        SetupHtmlResponse(fundaId, success: false); // Fails

        var scraper = GetScraperServiceWithMockedApi();

        // Act
        await scraper.ScrapeLimitedAsync("amsterdam", 1, CancellationToken.None);

        // Assert
        var listing = await DbContext.Listings.FirstOrDefaultAsync(l => l.FundaId == fundaId);

        Assert.NotNull(listing);
        Assert.Equal("1011AB", listing.PostalCode);
        Assert.Null(listing.Bedrooms); // Missing rich data
        Assert.Null(listing.LivingAreaM2); // Missing rich data
        Assert.Equal("Best Broker", listing.AgentName);
    }

    [Fact]
    public async Task Scrape_GracefulDegradation_ContactFails()
    {
        // Arrange
        var fundaId = "9004";
        var address = "NoContact St 4";

        SetupSearchResponse(fundaId, address);
        SetupSummaryResponse(fundaId, success: true);
        SetupContactResponse(fundaId, success: false); // Fails
        SetupHtmlResponse(fundaId, success: true);

        var scraper = GetScraperServiceWithMockedApi();

        // Act
        await scraper.ScrapeLimitedAsync("amsterdam", 1, CancellationToken.None);

        // Assert
        var listing = await DbContext.Listings.FirstOrDefaultAsync(l => l.FundaId == fundaId);

        Assert.NotNull(listing);
        Assert.Equal("1011AB", listing.PostalCode);
        Assert.Equal(4, listing.Bedrooms);
        Assert.Null(listing.BrokerPhone); // Missing contact details
        Assert.Null(listing.BrokerLogoUrl);
    }
}
