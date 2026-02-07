using System.Net;
using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
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

    private PlaywrightFundaClient CreateClient(Mock<HttpMessageHandler>? handler = null)
    {
        var factoryMock = new Mock<IHttpClientFactory>();
        handler ??= new Mock<HttpMessageHandler>();

        // Return a fresh HttpClient with the mocked handler
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>()))
                   .Returns(() => new HttpClient(handler.Object));

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
        var listings = new List<FundaApiListing>
        {
            new() { Price = "Prijs op aanvraag", ListingUrl = "/unknown-price" },
            new() { Price = "€ 900.000", ListingUrl = "/expensive" }
        };

        var filtered = (List<FundaApiListing>)ApplyPriceFilterMethod.Invoke(null, [listings, 100000, 800000])!;

        Assert.Single(filtered);
        Assert.Equal("/unknown-price", filtered[0].ListingUrl);
    }

    [Fact]
    public async Task GetListingSummaryAsync_ReturnsSummary_OnSuccess()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(@"{""identifiers"": { ""globalId"": 123 }, ""price"": { ""sellingPrice"": ""100000"" }}")
            });

        var client = CreateClient(handler);
        var result = await client.GetListingSummaryAsync(123);

        Assert.NotNull(result);
        Assert.Equal(123, result.Identifiers?.GlobalId);
        Assert.Equal("100000", result.Price?.SellingPrice);
    }

    [Fact]
    public async Task GetListingSummaryAsync_ReturnsNull_OnNotFound()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound
            });

        var client = CreateClient(handler);
        var result = await client.GetListingSummaryAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetContactDetailsAsync_ReturnsDetails_OnSuccess()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(@"{""contactBlockDetails"": [{ ""displayName"": ""Test Broker"" }]}")
            });

        var client = CreateClient(handler);
        var result = await client.GetContactDetailsAsync(123);

        Assert.NotNull(result);
        Assert.Equal("Test Broker", result.ContactDetails.FirstOrDefault()?.DisplayName);
    }

    [Fact]
    public async Task GetFiberAvailabilityAsync_ReturnsResponse_OnSuccess()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(@"{""availability"": true, ""postalCode"": ""1234AB""}")
            });

        var client = CreateClient(handler);
        var result = await client.GetFiberAvailabilityAsync("1234 AB");

        Assert.NotNull(result);
        Assert.True(result.Availability);
        Assert.Equal("1234AB", result.PostalCode);
    }
}
