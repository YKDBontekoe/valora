using Valora.Domain.Entities;
using Valora.Infrastructure.Scraping;
using Valora.Infrastructure.Scraping.Models;

namespace Valora.UnitTests.Scraping;

public class FundaMapperTests
{
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

        var result = FundaMapper.MapApiListingToDomain(apiListing, "123");

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

        FundaMapper.EnrichListingWithSummary(listing, summary);

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

        FundaMapper.EnrichListingWithNuxtData(listing, data);

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
    public void EnrichListingWithNuxtData_CVBoiler_NoYear_ParsesCorrectly()
    {
        var listing = new Listing { FundaId = "1", Address = "Test" };
        var data = new FundaNuxtListingData
        {
            Features = new FundaNuxtFeatures
            {
                Bouw = new FundaNuxtFeatureGroup
                {
                    KenmerkenList =
                    [
                        new() { Label = "CV-ketel", Value = "Intergas" }
                    ]
                }
            }
        };

        FundaMapper.EnrichListingWithNuxtData(listing, data);

        Assert.Equal("Intergas", listing.CVBoilerBrand);
        Assert.Null(listing.CVBoilerYear);
    }

    [Fact]
    public void EnrichListingWithNuxtData_NullData_DoesNotCrash()
    {
        var listing = new Listing { FundaId = "1", Address = "Test" };
        var data = new FundaNuxtListingData
        {
             // All null
        };

        FundaMapper.EnrichListingWithNuxtData(listing, data);

        // Should not throw and fields remain null
        // Note: The new logic initializes feature map even if empty, so checks if empty not null
        Assert.Empty(listing.Features!);
        Assert.Null(listing.Description);
    }

    [Fact]
    public void EnrichListingWithNuxtData_GardenArea_ParsesHighest()
    {
        var listing = new Listing { FundaId = "1", Address = "Test" };
        var data = new FundaNuxtListingData
        {
            Features = new FundaNuxtFeatures
            {
                Indeling = new FundaNuxtFeatureGroup
                {
                    KenmerkenList =
                    [
                        new() { Label = "Achtertuin", Value = "30 m²" },
                        new() { Label = "Voortuin", Value = "15 m²" }
                    ]
                }
            }
        };

        FundaMapper.EnrichListingWithNuxtData(listing, data);

        // Should pick highest garden area (30)
        Assert.Equal(30, listing.GardenM2);
    }

    [Fact]
    public void FlattenFeatures_NestedItems_RecursesCorrectly()
    {
        var listing = new Listing { FundaId = "1", Address = "Test" };
        var data = new FundaNuxtListingData
        {
            Features = new FundaNuxtFeatures
            {
                Indeling = new FundaNuxtFeatureGroup
                {
                    KenmerkenList =
                    [
                        new()
                        {
                            Label = "Badkamer",
                            Value = "Luxe",
                            KenmerkenList =
                            [
                                new() { Label = "Ligbad", Value = "Ja" }
                            ]
                        }
                    ]
                }
            }
        };

        FundaMapper.EnrichListingWithNuxtData(listing, data);

        Assert.NotNull(listing.Features);
        Assert.True(listing.Features.ContainsKey("Badkamer"));
        Assert.True(listing.Features.ContainsKey("Ligbad"));
        Assert.Equal("Ja", listing.Features["Ligbad"]);
    }

    [Fact]
    public void FlattenFeatures_GroupTitleNode_RecursesCorrectly()
    {
        var listing = new Listing { FundaId = "1", Address = "Test" };
        // Structure: No label on parent, only children
        var data = new FundaNuxtListingData
        {
            Features = new FundaNuxtFeatures
            {
                Indeling = new FundaNuxtFeatureGroup
                {
                    KenmerkenList =
                    [
                        new()
                        {
                            // Missing Label/Value, acts as group
                            KenmerkenList =
                            [
                                new() { Label = "Isolatie", Value = "Dubbel glas" }
                            ]
                        }
                    ]
                }
            }
        };

        FundaMapper.EnrichListingWithNuxtData(listing, data);

        Assert.NotNull(listing.Features);
        Assert.True(listing.Features.ContainsKey("Isolatie"));
        Assert.Equal("Dubbel glas", listing.Features["Isolatie"]);
    }

    [Fact]
    public void ParseFirstNumber_ParsesCorrectly()
    {
        Assert.Equal(123, FundaMapper.ParseFirstNumber("123 m²"));
        Assert.Equal(50, FundaMapper.ParseFirstNumber("€ 50.000"));
        Assert.Null(FundaMapper.ParseFirstNumber("Geen cijfers"));
    }

    [Fact]
    public void EnrichListingWithNuxtData_MiscellaneousFeatures_MapsCorrectly()
    {
        var listing = new Listing { FundaId = "1", Address = "Test" };
        var data = new FundaNuxtListingData
        {
            Features = new FundaNuxtFeatures
            {
                Indeling = new FundaNuxtFeatureGroup
                {
                    KenmerkenList =
                    [
                        new() { Label = "Tuin", Value = "Achtertuin" }, // Added first to ensure Ligging overrides it
                        new() { Label = "Gebouwgebonden buitenruimte", Value = "10 m²" },
                        new() { Label = "Externe bergruimte", Value = "5 m²" },
                        new() { Label = "Inhoud", Value = "400 m³" },
                        new() { Label = "Ligging", Value = "Aan water" },
                        new() { Label = "Garage", Value = "Aangebouwd steen" },
                        new() { Label = "Parkeerfaciliteiten", Value = "Openbaar parkeren" },
                        // Cadastral logic: key.Any(char.IsUpper) && key.Any(char.IsDigit) && key.Length > 5
                        // && !key.Contains("kamers") && !key.Contains("bouw")
                        // AND value is empty or "Title"
                        // "Kadastrale kaart 12345" has no Upper char! Let's fix that. "Kadastrale Kaart 12345"
                        // Also FlattenFeatures ignores empty values, so use "Title" which is the other condition
                        new() { Label = "Kadastrale Kaart 12345", Value = "Title" }
                    ]
                }
            }
        };

        FundaMapper.EnrichListingWithNuxtData(listing, data);

        Assert.Equal(10, listing.BalconyM2);
        Assert.Equal(5, listing.ExternalStorageM2);
        Assert.Equal(400, listing.VolumeM3);
        Assert.Equal("Aan water", listing.GardenOrientation);
        Assert.True(listing.HasGarage);
        Assert.Equal("Openbaar parkeren", listing.ParkingType);
        Assert.Equal("Kadastrale Kaart 12345", listing.CadastralDesignation);
    }
}
