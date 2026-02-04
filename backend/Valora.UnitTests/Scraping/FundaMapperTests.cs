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
        var apiListing = new FundaApiListing
        {
            ListingUrl = "/koop/amsterdam/huis-123",
            Price = "€ 500.000 k.k.",
            AgentName = "Top Agent",
            Address = new FundaApiAddress { City = "Amsterdam", ListingAddress = "Kerkstraat 1" },
            Image = new FundaApiImage { Default = "img.jpg" }
        };

        var method = _mapperType.GetMethod("MapApiListingToDomain", BindingFlags.Public | BindingFlags.Static);
        var result = (Listing)method!.Invoke(null, [apiListing, "123"])!;

        Assert.Equal("123", result.FundaId);
        Assert.Equal("Kerkstraat 1", result.Address);
        Assert.Equal("Amsterdam", result.City);
        Assert.Equal(500000, result.Price);
        Assert.Equal("https://www.funda.nl/koop/amsterdam/huis-123", result.Url);
        Assert.Equal("img.jpg", result.ImageUrl);
        Assert.Equal("Top Agent", result.AgentName);
    }

    [Fact]
    public void EnrichListingWithSummary_MapsFields()
    {
        var listing = new Listing { FundaId = "1", Address = "Test" };
        var summary = new FundaApiListingSummary
        {
            PublicationDate = new DateTime(2023, 1, 1),
            IsSoldOrRented = true,
            Labels = [ new() { Text = "Nieuw" } ],
            Address = new FundaApiSummaryAddress { PostalCode = "1234AB", City = "TestCity" },
            Tracking = new FundaApiTracking { Values = new FundaApiTrackingValues { Status = "verkocht" } }
        };

        var method = _mapperType.GetMethod("EnrichListingWithSummary", BindingFlags.Public | BindingFlags.Static);
        method!.Invoke(null, [listing, summary]);

        Assert.Equal(new DateTime(2023, 1, 1), listing.PublicationDate);
        Assert.True(listing.IsSoldOrRented);
        Assert.Single(listing.Labels);
        Assert.Equal("Nieuw", listing.Labels[0]);
        Assert.Equal("1234AB", listing.PostalCode);
        Assert.Equal("TestCity", listing.City);
        Assert.Equal("Verkocht", listing.Status);
    }

    [Fact]
    public void EnrichListingWithNuxtData_Comprehensive_MapsAllFields()
    {
        var listing = new Listing { FundaId = "1", Address = "Test" };
        var data = new FundaNuxtListingData
        {
            Description = new FundaNuxtDescription { Content = "Nice house" },
            Features = new FundaNuxtFeatures
            {
                Bouw = new FundaNuxtFeatureGroup
                {
                    KenmerkenList =
                    [
                        new() { Label = "Daktype", Value = "Zadeldak" },
                        new() { Label = "Bouwperiode", Value = "1906-1930" },
                        new() { Label = "Aantal woonlagen", Value = "3 woonlagen" },
                        new() { Label = "CV-ketel", Value = "Vaillant (2019)" }
                    ]
                }
            },
            Media = new FundaNuxtMedia
            {
                Items = [ new() { Id = "img1", Type = 1 } ]
            },
            Coordinates = new FundaNuxtCoordinates { Lat = 52.0, Lng = 4.0 },
            Videos = [ new() { Url = "vid.mp4" } ],
            Photos360 = [ new() { Url = "360.jpg" } ],
            FloorPlans = [ new() { Url = "floor.jpg" } ],
            BrochureUrl = "brochure.pdf",
            ObjectInsights = new FundaNuxtObjectInsights { Views = 100, Saves = 10 },
            LocalInsights = new FundaNuxtLocalInsights { Inhabitants = 5000, AvgPricePerM2 = 4000 },
            OpenHouseDates = [ new() { Date = new DateTime(2023, 5, 1) } ]
        };

        var method = _mapperType.GetMethod("EnrichListingWithNuxtData", BindingFlags.Public | BindingFlags.Static);
        method!.Invoke(null, [listing, data]);

        // Assertions
        Assert.Equal("Nice house", listing.Description);

        // Features
        Assert.Equal("Zadeldak", listing.RoofType);
        Assert.Equal("1906-1930", listing.ConstructionPeriod);
        Assert.Equal(3, listing.NumberOfFloors);
        Assert.Equal("Vaillant", listing.CVBoilerBrand);
        Assert.Equal(2019, listing.CVBoilerYear);

        // Media
        Assert.Single(listing.ImageUrls);
        Assert.Contains("img1", listing.ImageUrls[0]);
        Assert.Equal(52.0, listing.Latitude);
        Assert.Equal("vid.mp4", listing.VideoUrl);
        Assert.Equal("360.jpg", listing.VirtualTourUrl);
        Assert.Single(listing.FloorPlanUrls);
        Assert.Equal("floor.jpg", listing.FloorPlanUrls[0]);
        Assert.Equal("brochure.pdf", listing.BrochureUrl);

        // Insights
        Assert.Equal(100, listing.ViewCount);
        Assert.Equal(10, listing.SaveCount);
        Assert.Equal(5000, listing.NeighborhoodPopulation);
        Assert.Equal(4000, listing.NeighborhoodAvgPriceM2);

        // Dates
        Assert.Single(listing.OpenHouseDates);
        Assert.Equal(new DateTime(2023, 5, 1), listing.OpenHouseDates[0]);
    }

    [Fact]
    public void MergeListingDetails_UpdatesTarget()
    {
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
        method!.Invoke(null, [target, source]);

        Assert.Equal(3, target.Bedrooms);
        Assert.Equal("Verkocht", target.Status);
        Assert.Equal("0612345678", target.BrokerPhone);
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
