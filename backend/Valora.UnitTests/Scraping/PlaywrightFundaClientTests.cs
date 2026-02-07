using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Valora.Infrastructure.Scraping;
using Valora.Infrastructure.Scraping.Models;

namespace Valora.UnitTests.Scraping;

public class PlaywrightFundaClientTests
{
    private static readonly MethodInfo ParseSearchResultsJsonMethod =
        typeof(PlaywrightFundaClient).GetMethod("ParseSearchResultsJson", BindingFlags.Instance | BindingFlags.NonPublic)!;

    private static readonly MethodInfo ExtractGlobalIdFromUrlMethod =
        typeof(PlaywrightFundaClient).GetMethod("ExtractGlobalIdFromUrl", BindingFlags.Static | BindingFlags.NonPublic)!;

    private static readonly MethodInfo ApplyPriceFilterMethod =
        typeof(PlaywrightFundaClient).GetMethod("ApplyPriceFilter", BindingFlags.Static | BindingFlags.NonPublic)!;

    private PlaywrightFundaClient CreateClient()
    {
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(() => new HttpClient());
        return new PlaywrightFundaClient(new NullLogger<PlaywrightFundaClient>(), factoryMock.Object);
    }

    [Fact]
    public void ParseSearchResultsJson_ParsesListingsFromSearchResultsPath()
    {
        var client = CreateClient();
        const string json = """
        {
          "searchResults": {
            "listings": [
              {
                "globalId": 43239385,
                "price": "€ 500.000",
                "listingUrl": "/detail/koop/amsterdam/house/43239385/",
                "address": {
                  "listingAddress": "Kerkstraat 1",
                  "city": "Amsterdam"
                },
                "image": {
                  "default": "https://img.test/1.jpg"
                }
              }
            ]
          }
        }
        """;

        var listings = (List<FundaApiListing>)ParseSearchResultsJsonMethod.Invoke(client, [json])!;

        Assert.Single(listings);
        Assert.Equal(43239385, listings[0].GlobalId);
        Assert.Equal("€ 500.000", listings[0].Price);
        Assert.Equal("/detail/koop/amsterdam/house/43239385/", listings[0].ListingUrl);
        Assert.Equal("Kerkstraat 1", listings[0].Address?.ListingAddress);
        Assert.Equal("Amsterdam", listings[0].Address?.City);
        Assert.Equal("https://img.test/1.jpg", listings[0].Image?.Default);
    }

    [Fact]
    public void ParseSearchResultsJson_UsesFallbackFields()
    {
        var client = CreateClient();
        const string json = """
        {
          "listings": [
            {
              "id": 12345678,
              "url": "/koop/amsterdam/huis-12345678-address/",
              "address": {
                "street": "Fallback Street",
                "city": "Utrecht"
              }
            }
          ]
        }
        """;

        var listings = (List<FundaApiListing>)ParseSearchResultsJsonMethod.Invoke(client, [json])!;

        Assert.Single(listings);
        Assert.Equal(12345678, listings[0].GlobalId);
        Assert.Equal("/koop/amsterdam/huis-12345678-address/", listings[0].ListingUrl);
        Assert.Equal("Fallback Street", listings[0].Address?.ListingAddress);
        Assert.Equal("Utrecht", listings[0].Address?.City);
    }

    [Fact]
    public void ParseSearchResultsJson_ReturnsEmpty_OnInvalidJson()
    {
        var client = CreateClient();

        var listings = (List<FundaApiListing>)ParseSearchResultsJsonMethod.Invoke(client, ["{not valid json"])!;

        Assert.Empty(listings);
    }

    [Fact]
    public void ParseSearchResultsJson_SkipsItemsWithoutGlobalId()
    {
        var client = CreateClient();
        const string json = """
        {
          "searchResults": {
            "listings": [
              {
                "price": "€ 350.000",
                "listingUrl": "/detail/koop/amsterdam/house/no-id/"
              }
            ]
          }
        }
        """;

        var listings = (List<FundaApiListing>)ParseSearchResultsJsonMethod.Invoke(client, [json])!;

        Assert.Empty(listings);
    }

    [Theory]
    [InlineData("/detail/koop/amsterdam/appartement-name/43239385/", 43239385)]
    [InlineData("/koop/amsterdam/huis-12345678-address/", 12345678)]
    [InlineData("/detail/koop/amsterdam/no-id/", 0)]
    public void ExtractGlobalIdFromUrl_ParsesExpectedId(string url, int expected)
    {
        // Static method invocation, instance not needed but method is on class
        // Invoke(null, ...) works for static methods
        var globalId = (int)ExtractGlobalIdFromUrlMethod.Invoke(null, [url])!;
        Assert.Equal(expected, globalId);
    }

    [Fact]
    public async Task DisposeAsync_IsIdempotent_WhenNoBrowserWasCreated()
    {
        var client = CreateClient();

        await client.DisposeAsync();
        await client.DisposeAsync();

        Assert.True(true);
    }

    [Fact]
    public void ApplyPriceFilter_FiltersOutsideRequestedRange()
    {
        // Static method
        var listings = new List<FundaApiListing>
        {
            new() { Price = "€ 300.000", ListingUrl = "/a" },
            new() { Price = "€ 500.000", ListingUrl = "/b" },
            new() { Price = "€ 700.000", ListingUrl = "/c" }
        };

        var filtered = (List<FundaApiListing>)ApplyPriceFilterMethod.Invoke(null, [listings, 400000, 600000])!;

        Assert.Single(filtered);
        Assert.Equal("/b", filtered[0].ListingUrl);
    }

    [Fact]
    public void ApplyPriceFilter_KeepsListingsWithUnparseablePrice()
    {
        // Static method
        var listings = new List<FundaApiListing>
        {
            new() { Price = "Prijs op aanvraag", ListingUrl = "/unknown-price" },
            new() { Price = "€ 900.000", ListingUrl = "/expensive" }
        };

        var filtered = (List<FundaApiListing>)ApplyPriceFilterMethod.Invoke(null, [listings, 100000, 800000])!;

        Assert.Single(filtered);
        Assert.Equal("/unknown-price", filtered[0].ListingUrl);
    }
}
