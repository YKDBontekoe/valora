using System.Reflection;
using Valora.Domain.Entities;
using Valora.Infrastructure.Scraping;
using Valora.Infrastructure.Scraping.Models;

namespace Valora.UnitTests.Scraping;

public class FundaMapperTests
{
    private readonly Type _mapperType;

    public FundaMapperTests()
    {
        var assembly = typeof(FundaScraperService).Assembly;
        _mapperType = assembly.GetType("Valora.Infrastructure.Scraping.FundaMapper")!;
    }

    [Fact]
    public void MapApiListingToDomain_MapsBasicFields()
    {
        // Arrange
        var apiListing = new FundaApiListing
        {
            ListingUrl = "/koop/amsterdam/huis-123",
            Price = "€ 500.000 k.k.",
            AgentName = "Top Agent",
            Address = new FundaApiAddress { City = "Amsterdam", ListingAddress = "Kerkstraat 1" },
            Image = new FundaApiImage { Default = "img.jpg" }
        };

        var method = _mapperType.GetMethod("MapApiListingToDomain", BindingFlags.Public | BindingFlags.Static);

        // Act
        var result = (Listing)method!.Invoke(null, [apiListing, "123"])!;

        // Assert
        Assert.Equal("123", result.FundaId);
        Assert.Equal("Kerkstraat 1", result.Address);
        Assert.Equal("Amsterdam", result.City);
        Assert.Equal(500000, result.Price);
        Assert.Equal("https://www.funda.nl/koop/amsterdam/huis-123", result.Url);
        Assert.Equal("img.jpg", result.ImageUrl);
        Assert.Equal("Top Agent", result.AgentName);
    }

    [Fact]
    public void EnrichListingWithNuxtData_PopulatesFeaturesMap()
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

        var method = _mapperType.GetMethod("EnrichListingWithNuxtData", BindingFlags.Public | BindingFlags.Static);

        // Act
        method!.Invoke(null, [listing, data]);

        // Assert
        Assert.NotNull(listing.Features);
        Assert.True(listing.Features.ContainsKey("Bijzonderheid"));
        Assert.Equal("Monumentaal pand", listing.Features["Bijzonderheid"]);

        Assert.True(listing.Features.ContainsKey("Sauna"));
        Assert.Equal("Ja", listing.Features["Sauna"]);
    }

    [Fact]
    public void EnrichListingWithNuxtData_ParsesSpecificFields()
    {
        // Arrange
        var listing = new Listing { FundaId = "1", Address = "Test" };
        var data = new FundaNuxtListingData
        {
            Features = new FundaNuxtFeatures
            {
                Indeling = new FundaNuxtFeatureGroup
                {
                    KenmerkenList = [ new() { Label = "Aantal kamers", Value = "5 kamers (3 slaapkamers)" } ]
                },
                Energie = new FundaNuxtFeatureGroup
                {
                    KenmerkenList = [ new() { Label = "Energielabel", Value = "A" } ]
                }
            },
            ObjectType = new FundaNuxtObjectType
            {
                PropertySpecification = new FundaNuxtPropertySpecification
                {
                    SelectedArea = 120,
                    SelectedPlotArea = 200
                }
            }
        };

        var method = _mapperType.GetMethod("EnrichListingWithNuxtData", BindingFlags.Public | BindingFlags.Static);

        // Act
        method!.Invoke(null, [listing, data]);

        // Assert
        Assert.Equal(120, listing.LivingAreaM2);
        Assert.Equal(200, listing.PlotAreaM2);
        Assert.Equal(3, listing.Bedrooms); // Parsed from regex
        Assert.Equal("A", listing.EnergyLabel);
    }

    [Fact]
    public void MergeListingDetails_UpdatesTarget()
    {
        // Arrange
        var target = new Listing { FundaId = "1", Address = "Test", Price = 100000 };
        var source = new Listing
        {
            FundaId = "1",
            Address = "Test",
            Price = 200000,
            Bedrooms = 3,
            Status = "Verkocht",
            BrokerPhone = "0612345678"
        };

        var method = _mapperType.GetMethod("MergeListingDetails", BindingFlags.Public | BindingFlags.Static);

        // Act
        method!.Invoke(null, [target, source]);

        // Assert
        Assert.Equal(3, target.Bedrooms);
        Assert.Equal("Verkocht", target.Status);
        Assert.Equal("0612345678", target.BrokerPhone);
        // Price is NOT updated by MergeListingDetails
        Assert.Equal(100000, target.Price);
    }

    [Fact]
    public void ParseFirstNumber_ParsesCorrectly()
    {
        var method = _mapperType.GetMethod("ParseFirstNumber", BindingFlags.Public | BindingFlags.Static);

        Assert.Equal(123, method!.Invoke(null, ["123 m²"]));
        Assert.Equal(50, method.Invoke(null, ["€ 50.000"]));
        Assert.Null(method.Invoke(null, ["Geen cijfers"]));
    }
}
