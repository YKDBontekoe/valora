using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Valora.Application.Common.Interfaces;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;
using System.Globalization;
using Valora.Application.Scraping;
using Valora.Infrastructure.Scraping;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace Valora.IntegrationTests;

[Collection("TestcontainersDatabase")]
public class ScraperBusinessLogicTests : IAsyncLifetime
{
    private readonly TestcontainersDatabaseFixture _fixture;
    private readonly WireMockServer _server;
    private readonly ValoraDbContext _cleanupContext;

    // Track disposable resources
    private readonly List<IDisposable> _disposables = new();
    private readonly List<IAsyncDisposable> _asyncDisposables = new();

    public ScraperBusinessLogicTests(TestcontainersDatabaseFixture fixture)
    {
        _fixture = fixture;
        _server = WireMockServer.Start();

        if (_fixture.Factory == null)
            throw new InvalidOperationException("Factory not initialized");

        var scope = _fixture.Factory.Services.CreateScope();
        _cleanupContext = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
        _disposables.Add(scope);
    }

    public async Task InitializeAsync()
    {
        // Cleanup data before each test
        _cleanupContext.PriceHistories.RemoveRange(_cleanupContext.PriceHistories);
        _cleanupContext.Listings.RemoveRange(_cleanupContext.Listings);
        await _cleanupContext.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        _server.Stop();
        _server.Dispose();

        foreach (var disposable in _disposables)
        {
            disposable.Dispose();
        }

        foreach (var asyncDisposable in _asyncDisposables)
        {
            await asyncDisposable.DisposeAsync();
        }
    }

    private IServiceScope CreateScopeWithMockedApi()
    {
        // Create a custom scope with the mocked HTTP handler
        // We need to create a NEW factory instance derived from the original one
        // to inject the test services.
        var clientFactory = _fixture.Factory!.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddHttpClient<FundaApiClient>()
                        .ConfigurePrimaryHttpMessageHandler(() => new RedirectHandler(_server.Urls[0]));
            });
        });

        // Track the factory for disposal
        _asyncDisposables.Add(clientFactory);

        // We create a scope from this new factory
        var scope = clientFactory.Services.CreateScope();

        // Track the scope for disposal
        _disposables.Add(scope);

        return scope;
    }

    // Helper to configure WireMock
    private void SetupMockSearchResponse(string fundaId, string address, decimal price, string? city = "Amsterdam")
    {
        var apiResponseJson = $@"
        {{
            ""listings"": [
                {{
                    ""globalId"": {fundaId},
                    ""price"": ""€ {price.ToString("N0", CultureInfo.GetCultureInfo("nl-NL"))} k.k."",
                    ""isSinglePrice"": true,
                    ""listingUrl"": ""/koop/{city?.ToLower()}/huis-{fundaId}-{address.Replace(" ", "-").ToLower()}/"",
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

        _server.Reset();
        _server.Given(Request.Create().UsingPost().WithPath("/api/topposition/v2/search"))
               .RespondWith(Response.Create()
                   .WithStatusCode(200)
                   .WithHeader("Content-Type", "application/json")
                   .WithBody(apiResponseJson));
    }

    [Fact]
    public async Task ScrapeLimitedAsync_NewListing_ShouldCreateAndRecordHistory()
    {
        // Arrange
        var fundaId = "10001";
        var address = "New St 1";
        decimal price = 500000;
        SetupMockSearchResponse(fundaId, address, price);

        using var scope = CreateScopeWithMockedApi();
        var context = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
        var scraper = scope.ServiceProvider.GetRequiredService<IFundaScraperService>();

        // Act
        await scraper.ScrapeLimitedAsync("amsterdam", 1, CancellationToken.None);

        // Assert
        var listing = await context.Listings
            .Include(l => l.PriceHistory)
            .FirstOrDefaultAsync(l => l.FundaId == fundaId);

        Assert.NotNull(listing);
        Assert.Equal(price, listing.Price);
        Assert.Single(listing.PriceHistory);
        Assert.Equal(price, listing.PriceHistory.First().Price);
    }

    [Fact]
    public async Task ScrapeLimitedAsync_PriceChange_ShouldUpdateAndRecordHistory()
    {
        // Arrange
        var fundaId = "10002";
        var address = "Change St 2";
        decimal oldPrice = 400000;
        decimal newPrice = 450000;

        using var scope = CreateScopeWithMockedApi();
        var context = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();

        // Seed existing listing
        var existingListing = new Listing
        {
            FundaId = fundaId,
            Address = address,
            City = "Amsterdam",
            Price = oldPrice,
            Url = "http://original",
            PropertyType = "Woonhuis",
            Status = "Beschikbaar"
        };
        context.Listings.Add(existingListing);
        await context.SaveChangesAsync();

        // Add initial history
        context.PriceHistories.Add(new PriceHistory { ListingId = existingListing.Id, Price = oldPrice, RecordedAt = DateTime.UtcNow.AddDays(-1) });
        await context.SaveChangesAsync();

        // Setup Mock with new price
        SetupMockSearchResponse(fundaId, address, newPrice);

        var scraper = scope.ServiceProvider.GetRequiredService<IFundaScraperService>();

        // Act
        await scraper.ScrapeLimitedAsync("amsterdam", 1, CancellationToken.None);

        // Assert
        context.ChangeTracker.Clear();
        var listing = await context.Listings
            .Include(l => l.PriceHistory)
            .FirstOrDefaultAsync(l => l.FundaId == fundaId);

        Assert.NotNull(listing);
        Assert.Equal(newPrice, listing.Price);

        Assert.Equal(2, listing.PriceHistory.Count);
        Assert.Contains(listing.PriceHistory, ph => ph.Price == oldPrice);
        Assert.Contains(listing.PriceHistory, ph => ph.Price == newPrice);
    }

    [Fact]
    public async Task ScrapeLimitedAsync_NoChange_ShouldNotAddHistory()
    {
        // Arrange
        var fundaId = "10003";
        var address = "Same St 3";
        decimal price = 300000;

        using var scope = CreateScopeWithMockedApi();
        var context = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();

        var existingListing = new Listing
        {
            FundaId = fundaId,
            Address = address,
            City = "Amsterdam",
            Price = price,
            Url = "http://original",
            PropertyType = "Woonhuis",
            Status = "Beschikbaar"
        };
        context.Listings.Add(existingListing);
        await context.SaveChangesAsync();

        context.PriceHistories.Add(new PriceHistory { ListingId = existingListing.Id, Price = price });
        await context.SaveChangesAsync();

        SetupMockSearchResponse(fundaId, address, price);

        var scraper = scope.ServiceProvider.GetRequiredService<IFundaScraperService>();

        // Act
        await scraper.ScrapeLimitedAsync("amsterdam", 1, CancellationToken.None);

        // Assert
        context.ChangeTracker.Clear();
        var listing = await context.Listings
            .Include(l => l.PriceHistory)
            .FirstOrDefaultAsync(l => l.FundaId == fundaId);

        Assert.NotNull(listing);
        Assert.Single(listing.PriceHistory); // Should still be 1
    }

    [Fact]
    public async Task ScrapeLimitedAsync_PartialData_ShouldNotOverwriteExisting()
    {
        // Arrange
        var fundaId = "10004";
        var address = "Partial St 4";
        decimal price = 600000;

        using var scope = CreateScopeWithMockedApi();
        var context = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();

        var existingListing = new Listing
        {
            FundaId = fundaId,
            Address = address,
            City = "Amsterdam",
            Price = price,
            Url = "http://original",
            PropertyType = "Woonhuis",
            Status = "Beschikbaar",
            Bedrooms = 3,           // Existing data
            LivingAreaM2 = 120      // Existing data
        };
        context.Listings.Add(existingListing);
        await context.SaveChangesAsync();

        // Mock response (API doesn't return bedrooms/living area in our mock setup)
        SetupMockSearchResponse(fundaId, address, price);

        var scraper = scope.ServiceProvider.GetRequiredService<IFundaScraperService>();

        // Act
        await scraper.ScrapeLimitedAsync("amsterdam", 1, CancellationToken.None);

        // Assert
        context.ChangeTracker.Clear();
        var listing = await context.Listings.FirstOrDefaultAsync(l => l.FundaId == fundaId);

        Assert.NotNull(listing);
        Assert.Equal(3, listing.Bedrooms);        // Should be preserved
        Assert.Equal(120, listing.LivingAreaM2);  // Should be preserved
    }
}
