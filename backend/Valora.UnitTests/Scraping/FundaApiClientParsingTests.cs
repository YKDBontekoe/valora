using System.Net;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Valora.Infrastructure.Scraping;

namespace Valora.UnitTests.Scraping;

public class FundaApiClientParsingTests
{
    private readonly Mock<HttpMessageHandler> _httpHandlerMock;
    private readonly FundaApiClient _client;

    public FundaApiClientParsingTests()
    {
        _httpHandlerMock = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(_httpHandlerMock.Object);
        var loggerMock = new Mock<ILogger<FundaApiClient>>();
        
        _client = new FundaApiClient(httpClient, loggerMock.Object);
    }

    [Fact]
    public async Task GetListingDetailsAsync_NoMatchingScript_ReturnsNull()
    {
        // Arrange
        var html = $@"
        <html>
        <body>
            <script type=""application/json"">
                {{ ""irrelevant"": true }}
            </script>
        </body>
        </html>";

        _httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(html)
            });

        // Act
        var result = await _client.GetListingDetailsAsync("https://www.funda.nl/koop/nope", CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetListingDetailsAsync_ParsesNuxtStateCorrectly()
    {
        // Arrange
        var jsonState = @"
        [
            ""SomeMeta"",
            {
                ""features"": {
                    ""indeling"": { 
                        ""KenmerkenList"": [
                            { ""Label"": ""Aantal kamers"", ""Value"": ""5 kamers (3 slaapkamers)"" },
                            { ""Label"": ""Aantal badkamers"", ""Value"": ""2 badkamers"" }
                        ]
                    },
                    ""afmetingen"": {
                        ""KenmerkenList"": [
                            { ""Label"": ""Wonen"", ""Value"": ""120 m²"" }
                        ]
                    },
                    ""energie"": { 
                         ""KenmerkenList"": [
                            { ""Label"": ""Energielabel"", ""Value"": ""A"" }
                        ]
                    }
                },
                ""media"": {
                    ""items"": [
                        { ""id"": ""12345"", ""type"": 1 },
                        { ""id"": ""67890"", ""type"": 1 }
                    ]
                },
                ""description"": {
                    ""content"": ""Prachtig huis te koop.""
                }
            }
        ]";

        var html = $@"
        <html>
        <head></head>
        <body>
            <h1>Test Content</h1>
            <script type=""application/json"" data-nuxt-data=""listing-detail"">
                {jsonState}
            </script>
        </body>
        </html>";

        _httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(html)
            });

        // Act
        var result = await _client.GetListingDetailsAsync("https://www.funda.nl/koop/amsterdam/huis-123", CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Features);
        Assert.NotNull(result.Description);
        Assert.Equal("Prachtig huis te koop.", result.Description.Content);
        
        // Check Features (List based now)
        Assert.NotNull(result.Features.Indeling!.KenmerkenList);
        Assert.Contains(result.Features.Indeling.KenmerkenList, i => i.Label == "Aantal kamers" && i.Value == "5 kamers (3 slaapkamers)");
        
        // Check Media
        Assert.NotNull(result.Media);
        Assert.Equal(2, result.Media.Items.Count);
        Assert.Equal("12345", result.Media.Items[0].Id);
    }

    [Fact]
    public async Task GetListingDetailsAsync_MultipleScripts_ExtractsCorrectOne()
    {
        // Arrange
        var targetJson = @"{ ""description"": { ""content"": ""Correct"" }, ""features"": {}, ""media"": {} }";

        var html = $@"
        <html>
        <body>
            <script type=""application/json"">
                {{ ""irrelevant"": true }}
            </script>
            <script type=""application/json"">
                {{ ""cachedListingData"": true, {targetJson.Trim('{', '}')} }}
            </script>
            <script type=""application/json"">
                {{ ""other"": ""data"" }}
            </script>
        </body>
        </html>";

        _httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(html)
            });

        // Act
        var result = await _client.GetListingDetailsAsync("https://www.funda.nl/koop/yep", CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Correct", result!.Description?.Content);
    }

    [Fact]
    public async Task GetListingSummaryAsync_ShouldReturnSummary()
    {
        // Arrange
        var json = @"{ ""identifiers"": { ""globalId"": 123 }, ""address"": { ""title"": ""Street 1"" } }";
        SetupResponse(json);

        // Act
        var result = await _client.GetListingSummaryAsync(123);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(123, result!.Identifiers?.GlobalId);
        Assert.Equal("Street 1", result.Address?.Street);
    }

    [Fact]
    public async Task GetContactDetailsAsync_ShouldReturnContacts()
    {
        // Arrange
        var json = @"{ ""id"": ""c1"", ""contactBlockDetails"": [ { ""id"": 1, ""displayName"": ""Broker"" } ] }";
        SetupResponse(json);

        // Act
        var result = await _client.GetContactDetailsAsync(123);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result!.ContactDetails);
        Assert.Equal("Broker", result.ContactDetails[0].DisplayName);
    }

    [Fact]
    public async Task GetFiberAvailabilityAsync_ShouldReturnAvailability()
    {
        // Arrange
        var json = @"{ ""postalCode"": ""1234AB"", ""availability"": true }";
        SetupResponse(json);

        // Act
        var result = await _client.GetFiberAvailabilityAsync("1234AB");

        // Assert
        Assert.NotNull(result);
        Assert.True(result!.Availability);
    }

    [Fact]
    public async Task SearchBuyAsync_ShouldReturnListings()
    {
        // Arrange
        var json = @"{ ""listings"": [ { ""globalId"": 1, ""listingUrl"": ""url1"" } ] }";
        SetupResponse(json);

        // Act
        var result = await _client.SearchBuyAsync("amsterdam");

        // Assert
        Assert.NotNull(result);
        Assert.Single(result!.Listings);
        Assert.Equal(1, result.Listings[0].GlobalId);
    }

    [Fact]
    public async Task SearchAllBuyPagesAsync_PartialFailure_ShouldReturnSuccessResults()
    {
        // Arrange
        var jsonPage1 = @"{ ""listings"": [ { ""globalId"": 101 } ] }";
        var jsonPage3 = @"{ ""listings"": [ { ""globalId"": 102 } ] }";

        _httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.Content != null && r.Content.ReadAsStringAsync().Result.Contains("\"page\":1")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonPage1)
            });

        _httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.Content != null && r.Content.ReadAsStringAsync().Result.Contains("\"page\":2")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ThrowsAsync(new HttpRequestException("Simulated Network Error"));

         _httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.Content != null && r.Content.ReadAsStringAsync().Result.Contains("\"page\":3")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonPage3)
            });

        // Act
        var result = await _client.SearchAllBuyPagesAsync("amsterdam", 3);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains(result, l => l.GlobalId == 101);
        Assert.Contains(result, l => l.GlobalId == 102);
    }

    private void SetupResponse(string jsonContent)
    {
        _httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonContent)
            });
    }
}
