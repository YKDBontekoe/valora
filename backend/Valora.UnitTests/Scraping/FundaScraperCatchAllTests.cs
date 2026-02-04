using System.Reflection;
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
        
        // Use reflection on FundaMapper (internal class)
        var assembly = typeof(FundaScraperService).Assembly;
        var type = assembly.GetType("Valora.Infrastructure.Scraping.FundaMapper");
        Assert.NotNull(type);

        var method = type.GetMethod("EnrichListingWithNuxtData",
            BindingFlags.Public | BindingFlags.Static);
        
        Assert.NotNull(method);

        // Act
        method.Invoke(null, [listing, data]);

        // Assert
        Assert.NotNull(listing.Features);
        Assert.True(listing.Features.ContainsKey("Bijzonderheid"));
        Assert.Equal("Monumentaal pand", listing.Features["Bijzonderheid"]);
        
        Assert.True(listing.Features.ContainsKey("Sauna"));
        Assert.Equal("Ja", listing.Features["Sauna"]);
    }
}
