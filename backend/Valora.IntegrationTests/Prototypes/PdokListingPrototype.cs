using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Application.Enrichment;
using Valora.Infrastructure.Services;
using Xunit;
using Xunit.Abstractions;

namespace Valora.IntegrationTests.Prototypes;

public class PdokListingPrototype
{
    private readonly ITestOutputHelper _output;

    public PdokListingPrototype(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task GetListingDetails_ShouldFallbackToCbs_WhenWozScraperFails()
    {
        // 1. Arrange Mocks
        
        // Mock HttpClient to return fake PDOK response
        // This ensures we don't rely on external API for the lookup part
        var pdokResponseJson = @"
        {
            ""response"": {
                ""docs"": [
                    {
                        ""weergavenaam"": ""Kerkstraat 1"",
                        ""woonplaatsnaam"": ""Amsterdam"",
                        ""postcode"": ""1017GA"",
                        ""centroide_ll"": ""POINT(4.889 52.365)"",
                        ""bouwjaar"": ""1905"",
                        ""oppervlakte"": ""120"",
                        ""gebruiksdoelverblijfsobject"": ""woonfunctie"",
                        ""nummeraanduiding_id"": ""0363200000163643""
                    }
                ]
            }
        }";

        var fakeHandler = new FakeHttpMessageHandler(pdokResponseJson);
        var httpClient = new HttpClient(fakeHandler);
        
        var cache = new MemoryCache(new MemoryCacheOptions());
        var logger = new Mock<ILogger<PdokListingService>>();
        var options = Options.Create(new ContextEnrichmentOptions { 
            PdokBaseUrl = "https://api.pdok.nl",
            ReportCacheMinutes = 1
        });

        // Mock Scraper to Fail
        var wozMock = new Mock<IWozValuationService>();
        wozMock.Setup(x => x.GetWozValuationAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync((WozValuationDto?)null);

        // Mock Context Service to Provide Fallback
        var contextMock = new Mock<IContextReportService>();
        var fallbackReport = new ContextReportDto(
            Location: new ResolvedLocationDto("Query", "Address", 0, 0, 0, 0, null, null, null, null, null, null, null),
            SocialMetrics: new List<ContextMetricDto> { 
                new ContextMetricDto("average_woz", "Avg WOZ", 450.0, "k€", 100, "CBS", null) // 450k
            },
            CrimeMetrics: new List<ContextMetricDto>(), 
            DemographicsMetrics: new List<ContextMetricDto>(), 
            HousingMetrics: new List<ContextMetricDto>(),
            MobilityMetrics: new List<ContextMetricDto>(),
            AmenityMetrics: new List<ContextMetricDto>(), 
            EnvironmentMetrics: new List<ContextMetricDto>(),
            CompositeScore: 8.0, 
            CategoryScores: new Dictionary<string,double>(), 
            Sources: new List<SourceAttributionDto>(), 
            Warnings: new List<string>()
        );

        contextMock.Setup(x => x.BuildAsync(It.IsAny<ContextReportRequestDto>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(fallbackReport);

        var service = new PdokListingService(
            httpClient, cache, contextMock.Object, options, TimeProvider.System, logger.Object
        );

        string pdokId = "dummy-id"; 

        // Act
        var listing = await service.GetListingDetailsAsync(pdokId);

        // Assert
        Assert.NotNull(listing);
        _output.WriteLine($"WOZ Value: {listing.WozValue}");
        _output.WriteLine($"WOZ Source: {listing.WozValueSource}");

        Assert.Equal(450000, listing.WozValue); // 450.0 * 1000
        Assert.Equal("CBS Neighborhood Average", listing.WozValueSource);
    }

    public class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _response;
        public FakeHttpMessageHandler(string response) => _response = response;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(_response, System.Text.Encoding.UTF8, "application/json")
            });
        }
    }
}
