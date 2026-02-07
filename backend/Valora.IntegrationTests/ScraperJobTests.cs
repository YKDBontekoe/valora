using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Valora.Application.Common.Interfaces;
using Valora.Application.Scraping;
using Valora.Infrastructure.Persistence;
using Valora.Infrastructure.Scraping;
using WireMock.Matchers;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace Valora.IntegrationTests;

[Collection("TestDatabase")]
public class ScraperJobTests : IAsyncLifetime
{
    private readonly TestDatabaseFixture _fixture;
    private readonly WireMockServer _server;
    private IServiceScope _scope;
    private ValoraDbContext _context;
    private IFundaScraperService _scraperService;
    private IDisposable _clientFactory;

    public ScraperJobTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
        _server = WireMockServer.Start();
    }

    public async Task InitializeAsync()
    {
        if (_fixture.Factory == null)
            throw new InvalidOperationException("Factory not initialized");

        // Clean up database before each test
        using var cleanupScope = _fixture.Factory.Services.CreateScope();
        var context = cleanupScope.ServiceProvider.GetRequiredService<ValoraDbContext>();
        context.Listings.RemoveRange(context.Listings);
        await context.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        _scope?.Dispose();
        if (_clientFactory is IAsyncDisposable asyncDisposable)
            await asyncDisposable.DisposeAsync();
        else
            _clientFactory?.Dispose();

        _server.Stop();
        _server.Dispose();
    }

    private void ConfigureScraper(List<string> searchUrls)
    {
        // Create a custom factory that replaces services
        var factory = _fixture.Factory!.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                // Configure Options
                services.Configure<ScraperOptions>(options =>
                {
                    options.SearchUrls = searchUrls;
                    options.DelayBetweenRequestsMs = 0; // Speed up tests
                });

                // Mock HttpClient
                services.AddHttpClient<IFundaApiClient, FundaApiClient>()
                        .ConfigurePrimaryHttpMessageHandler(() => new RedirectHandler(_server.Urls[0]));
            });
        });

        _clientFactory = factory;
        _scope = factory.Services.CreateScope();
        _context = _scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
        _scraperService = _scope.ServiceProvider.GetRequiredService<IFundaScraperService>();
    }

    private void SetupMockSearchResponse(string region, string fundaId, string address)
    {
        var apiResponseJson = $@"
        {{
            ""listings"": [
                {{
                    ""globalId"": {fundaId},
                    ""price"": ""€ 500.000 k.k."",
                    ""isSinglePrice"": true,
                    ""listingUrl"": ""/koop/{region}/huis-{fundaId}-test/"",
                    ""image"": {{ ""default"": ""https://test.jpg"" }},
                    ""address"": {{ ""listingAddress"": ""{address}"", ""city"": ""{region}"" }},
                    ""isProject"": false
                }}
            ]
        }}";

        // Match Page 1 specifically
        _server.Given(Request.Create()
               .UsingPost()
               .WithPath("/api/topposition/v2/search")
               .WithBody(new RegexMatcher($"(?i)\"GeoInformation\":\\s*\"{region}\""))
               .WithBody(new RegexMatcher($"(?i)\"Page\":\\s*1")))
               .RespondWith(Response.Create()
                   .WithStatusCode(200)
                   .WithHeader("Content-Type", "application/json")
                   .WithBody(apiResponseJson));

        // Match other pages (return empty)
        _server.Given(Request.Create()
               .UsingPost()
               .WithPath("/api/topposition/v2/search")
               .WithBody(new RegexMatcher($"(?i)\"GeoInformation\":\\s*\"{region}\""))
               .WithBody(new RegexMatcher($"(?i)\"Page\":\\s*[2345]")))
               .RespondWith(Response.Create()
                   .WithStatusCode(200)
                   .WithHeader("Content-Type", "application/json")
                   .WithBody("{ \"listings\": [] }"));

        // Mock Summary API (required for enrichment not to fail, though we test graceful degradation elsewhere)
        _server.Given(Request.Create().UsingGet().WithPath(p => p.Contains($"/api/detail-summary/v2/getsummary/{fundaId}")))
               .RespondWith(Response.Create().WithStatusCode(200).WithBody("{}"));
    }

    private void SetupMockFailure(string region)
    {
        _server.Given(Request.Create()
               .UsingPost()
               .WithPath("/api/topposition/v2/search")
               .WithBody(new RegexMatcher($"(?i)\"GeoInformation\":\\s*\"{region}\"")))
               .RespondWith(Response.Create().WithStatusCode(500));
    }

    [Fact]
    public async Task ScrapeAndStoreAsync_MultipleUrls_ShouldProcessAll()
    {
        // Arrange
        var urls = new List<string> { "https://www.funda.nl/koop/amsterdam/", "https://www.funda.nl/koop/rotterdam/" };
        ConfigureScraper(urls);

        SetupMockSearchResponse("amsterdam", "1001", "Amsterdam St 1");
        SetupMockSearchResponse("rotterdam", "1002", "Rotterdam St 2");

        // Act
        await _scraperService.ScrapeAndStoreAsync(CancellationToken.None);

        // Assert
        var listings = await _context.Listings.ToListAsync();
        Assert.Equal(2, listings.Count);
        Assert.Contains(listings, l => l.FundaId == "1001");
        Assert.Contains(listings, l => l.FundaId == "1002");
    }

    [Fact]
    public async Task ScrapeAndStoreAsync_PartialFailure_ShouldContinue()
    {
        // Arrange
        var urls = new List<string> { "https://www.funda.nl/koop/amsterdam/", "https://www.funda.nl/koop/rotterdam/" };
        ConfigureScraper(urls);

        SetupMockSearchResponse("amsterdam", "1001", "Amsterdam St 1");
        SetupMockFailure("rotterdam");

        // Act
        await _scraperService.ScrapeAndStoreAsync(CancellationToken.None);

        // Assert
        var listings = await _context.Listings.ToListAsync();
        Assert.Single(listings);
        Assert.Contains(listings, l => l.FundaId == "1001");
    }
}
