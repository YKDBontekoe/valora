using Valora.Domain.Entities;
using Valora.Infrastructure.Scraping;
using Valora.Infrastructure.Scraping.Models;

namespace Valora.UnitTests.Scraping;

public class FundaScraperCatchAllTests
{
    [Fact]
    public void EnrichListing_PopulatesFeaturesMap()
    {
        // Arrange
        var listing = new Listing { FundaId = "1", Address = "Test" };
        var data = new FundaNuxtListingData
        {
            Features = new FundaNuxtFeatures
            {
                Indeling = new FundaNuxtFeatureGroup
                {
                    KenmerkenList =
                    [
                        new() { Label = "Bijzonderheid", Value = "Monumentaal pand" },
                        new() { Label = "Sauna", Value = "Ja" }
                    ]
                }
            }
        };
        
        // Reflection to access private static method if needed, or we just trust the integration.
        // For unit testing private methods, typically we make them internal or use "InternalsVisibleTo".
        // However, since we modified the class, we can check if we should make the enricher public or test indirectly.
        // Given complexity, let's assume we can't easily call private static without reflection.
        // BUT, `FundaScraperService` is the SUT. We can verify the logic via a "Testable" subclass or reflection.
        
        // Let's use reflection for this specific logic verification
        var method = typeof(FundaScraperService).GetMethod("EnrichListingWithNuxtData", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            
        // Act
        method!.Invoke(null, [listing, data]);

        // Assert
        Assert.NotNull(listing.Features);
        Assert.True(listing.Features.ContainsKey("Bijzonderheid"));
        Assert.Equal("Monumentaal pand", listing.Features["Bijzonderheid"]);
        
        Assert.True(listing.Features.ContainsKey("Sauna"));
        Assert.Equal("Ja", listing.Features["Sauna"]);
    }
}
