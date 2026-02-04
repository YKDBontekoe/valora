using System.Text.Json;
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

public class ScraperEnrichmentIntegrationTests : BaseIntegrationTest, IDisposable
{
    private readonly WireMockServer _server;

    public ScraperEnrichmentIntegrationTests(TestDatabaseFixture fixture) : base(fixture)
    {
        _server = WireMockServer.Start();
    }

    [Fact]
    public async Task ScrapeLimitedAsync_ShouldEnrichListingWithDetails()
    {
        // Arrange
        var fundaId = "999999";
        var address = "Enriched Street 1";
        var city = "Amsterdam";
        var listingUrlPath = $"/koop/amsterdam/huis-{fundaId}-enriched-street-1/";
        var fullListingUrl = $"https://www.funda.nl{listingUrlPath}";
        var postalCode = "1234AB";

        // 1. Mock Search API
        var apiResponseJson = $@"
        {{
            ""listings"": [
                {{
                    ""globalId"": {fundaId},
                    ""price"": ""€ 750.000 k.k."",
                    ""isSinglePrice"": true,
                    ""listingUrl"": ""{listingUrlPath}"",
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

        // 2. Mock Summary API
        var summaryJson = $@"
        {{
            ""identifiers"": {{ ""globalId"": {fundaId} }},
            ""publicationDate"": ""2023-10-01T10:00:00Z"",
            ""isSoldOrRented"": false,
            ""address"": {{
                ""postCode"": ""{postalCode}"",
                ""city"": ""{city}""
            }},
            ""tracking"": {{
                ""values"": {{
                    ""listing_status"": ""beschikbaar""
                }}
            }},
            ""labels"": [
                {{ ""text"": ""Open Huis"", ""type"": ""promolabel"" }}
            ]
        }}";

        _server.Given(Request.Create().UsingGet().WithPath($"/api/detail-summary/v2/getsummary/{fundaId}"))
               .RespondWith(Response.Create()
                   .WithStatusCode(200)
                   .WithBody(summaryJson));

        // 3. Mock Contact API
        var contactJson = $@"
        {{
            ""id"": ""{fundaId}"",
            ""listingId"": {fundaId},
            ""contactBlockDetails"": [
                {{
                    ""id"": 555,
                    ""displayName"": ""Best Brokers"",
                    ""phoneNumber"": ""020-1234567"",
                    ""logoUrl"": ""https://logo.url"",
                    ""associationCode"": ""NVM"",
                    ""isContactingEnabled"": true
                }}
            ]
        }}";

        _server.Given(Request.Create().UsingGet().WithPath($"/api/v3/listings/{fundaId}/contact-details"))
               .RespondWith(Response.Create()
                   .WithStatusCode(200)
                   .WithBody(contactJson));

        // 4. Mock Fiber API
        var fiberJson = $@"
        {{
            ""postalCode"": ""{postalCode}"",
            ""availability"": true,
            ""message"": ""Fiber is available""
        }}";

        _server.Given(Request.Create().UsingGet().WithPath($"/api/v1/{postalCode}"))
               .RespondWith(Response.Create()
                   .WithStatusCode(200)
                   .WithBody(fiberJson));

        // 5. Mock Listing Page HTML (Nuxt)
        // We construct a JSON object matching FundaNuxtListingData
        var nuxtData = new
        {
            description = new { content = "A beautiful house with enrichment." },
            objectType = new
            {
                propertyspecification = new
                {
                    selectedArea = 150, // LivingAreaM2
                    selectedPlotArea = 300 // PlotAreaM2
                }
            },
            features = new
            {
                indeling = new
                {
                    Title = "Indeling",
                    KenmerkenList = new[]
                    {
                        new { Label = "Aantal kamers", Value = "5 kamers (4 slaapkamers)" },
                        new { Label = "Aantal badkamers", Value = "2 badkamers" }
                    }
                },
                energie = new
                {
                    Title = "Energie",
                    KenmerkenList = new[]
                    {
                        new { Label = "Energielabel", Value = "A" },
                        new { Label = "Isolatie", Value = "Volledig geïsoleerd" },
                        new { Label = "Verwarming", Value = "Vloerverwarming" }
                    }
                },
                bouw = new
                {
                    Title = "Bouw",
                    KenmerkenList = new[]
                    {
                        new { Label = "Bouwjaar", Value = "2020" },
                        new { Label = "Dak", Value = "Zadeldak" }
                    }
                }
            },
            media = new
            {
                items = new[]
                {
                    new { id = "12345", type = 1 }
                }
            }
        };

        var nuxtJson = JsonSerializer.Serialize(nuxtData);
        var htmlContent = $@"
        <html>
            <body>
                <h1>Test House</h1>
                <script type=""application/json"">
                    {nuxtJson}
                </script>
            </body>
        </html>";

        _server.Given(Request.Create().UsingGet().WithPath(listingUrlPath))
               .RespondWith(Response.Create()
                   .WithStatusCode(200)
                   .WithBody(htmlContent));

        // Act
        using var clientFactory = Factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddHttpClient<FundaApiClient>()
                        .ConfigurePrimaryHttpMessageHandler(() => new RedirectHandler(_server.Urls[0]));
            });
        });

        using var scope = clientFactory.Services.CreateScope();
        var scraperService = scope.ServiceProvider.GetRequiredService<IFundaScraperService>();

        await scraperService.ScrapeLimitedAsync("amsterdam", 1, CancellationToken.None);

        // Assert
        // Verify WireMock received the request for the HTML page
        var logs = _server.LogEntries.Where(l => l.RequestMessage.Path == listingUrlPath).ToList();
        Assert.NotEmpty(logs); // Ensure the scraper actually tried to fetch the page

        var listingRepository = scope.ServiceProvider.GetRequiredService<IListingRepository>();
        var listing = await listingRepository.GetByFundaIdAsync(fundaId);

        Assert.NotNull(listing);

        // Basic fields
        Assert.Equal(fundaId, listing.FundaId);
        Assert.Equal(address, listing.Address);
        Assert.Equal(city, listing.City);
        Assert.Equal(750000, listing.Price);

        // Enriched Fields (Summary)
        Assert.Equal(postalCode, listing.PostalCode);
        Assert.NotNull(listing.PublicationDate);
        Assert.Equal(new DateTime(2023, 10, 1, 10, 0, 0, DateTimeKind.Utc), listing.PublicationDate);
        Assert.Contains("Open Huis", listing.Labels);
        Assert.Equal("Beschikbaar", listing.Status);

        // Enriched Fields (Contact)
        Assert.Equal("Best Brokers", listing.AgentName);
        Assert.Equal("020-1234567", listing.BrokerPhone);
        Assert.Equal("NVM", listing.BrokerAssociationCode);

        // Enriched Fields (Fiber)
        Assert.True(listing.FiberAvailable);

        // Enriched Fields (Nuxt/HTML)
        Assert.Equal("A beautiful house with enrichment.", listing.Description);
        Assert.Equal(150, listing.LivingAreaM2);
        Assert.Equal(300, listing.PlotAreaM2);
        Assert.Equal(4, listing.Bedrooms);
        Assert.Equal(2, listing.Bathrooms);
        Assert.Equal("A", listing.EnergyLabel);
        Assert.Equal("Volledig geïsoleerd", listing.InsulationType);
        Assert.Equal("Vloerverwarming", listing.HeatingType);
        Assert.Equal(2020, listing.YearBuilt);
        Assert.Equal("Zadeldak", listing.RoofType);
    }

    public void Dispose()
    {
        _server.Stop();
        _server.Dispose();
    }
}
