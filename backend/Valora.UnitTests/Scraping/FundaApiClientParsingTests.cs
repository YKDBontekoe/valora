using System.Net;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Valora.Domain.Entities;
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

        SetupHtmlResponse(html);

        // Act
        var result = await _client.GetListingDetailsAsync("https://example.com", CancellationToken.None);

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

        SetupHtmlResponse(html);

        // Act
        var result = await _client.GetListingDetailsAsync("https://www.funda.nl/koop/amsterdam/huis-123", CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Prachtig huis te koop.", result.Description);
        
        // Check Features (mapped to Dictionary)
        Assert.Contains(result.Features, kvp => kvp.Key == "Aantal kamers" && kvp.Value == "5 kamers (3 slaapkamers)");
        Assert.Contains(result.Features, kvp => kvp.Key == "Energielabel" && kvp.Value == "A");
        Assert.Equal(120, result.LivingAreaM2);
        
        // Check Media
        Assert.NotEmpty(result.ImageUrls);
        Assert.Equal(2, result.ImageUrls.Count);
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

        SetupHtmlResponse(html);

        // Act
        var result = await _client.GetListingDetailsAsync("https://example.com", CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Correct", result!.Description);
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
        Assert.Equal("123", result!.FundaId);
        // Address mapping check logic
    }

    [Fact]
    public async Task GetListingSummaryAsync_ShouldMapAddressCorrectly()
    {
        // Arrange
        var json = @"{ ""identifiers"": { ""globalId"": 123 }, ""address"": { ""street"": ""Main St 1"", ""city"": ""Amsterdam"" } }";
        SetupResponse(json);

        // Act
        var result = await _client.GetListingSummaryAsync(123);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("123", result!.FundaId);
        Assert.Equal("Amsterdam", result.City);
    }

    [Fact]
    public async Task GetContactDetailsAsync_ShouldReturnContacts()
    {
        // Arrange
        var json = @"{ ""id"": ""c1"", ""contactBlockDetails"": [ { ""id"": 1, ""displayName"": ""Broker"", ""phoneNumber"": ""123"" } ] }";
        SetupResponse(json);

        // Act
        var result = await _client.GetContactDetailsAsync(123);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Broker", result!.AgentName);
        Assert.Equal("123", result.BrokerPhone);
    }

    [Fact]
    public async Task CheckFiberAvailabilityAsync_ShouldReturnAvailability()
    {
        // Arrange
        var json = @"{ ""postalCode"": ""1234AB"", ""availability"": true }";
        SetupResponse(json);

        // Act
        var result = await _client.CheckFiberAvailabilityAsync("1234AB");

        // Assert
        Assert.NotNull(result);
        Assert.True(result!.Value);
    }

    [Fact]
    public async Task SearchBuyAsync_ShouldReturnListings()
    {
        // Arrange
        var json = @"{ ""listings"": [ { ""globalId"": 1, ""listingUrl"": ""url1"", ""address"": { ""city"": ""Amsterdam"" } } ] }";
        SetupResponse(json);

        // Act
        var result = await _client.SearchBuyAsync("amsterdam");

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("1", result[0].FundaId);
        Assert.Equal("Amsterdam", result[0].City);
    }

    [Fact]
    public async Task SearchRentAsync_ShouldReturnListings()
    {
        // Arrange
        var json = @"{ ""listings"": [ { ""globalId"": 2, ""listingUrl"": ""url2"", ""address"": { ""city"": ""Rotterdam"" } } ] }";
        SetupResponse(json);

        // Act
        var result = await _client.SearchRentAsync("rotterdam");

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("2", result[0].FundaId);
        Assert.Equal("Rotterdam", result[0].City);
    }

    [Fact]
    public async Task SearchProjectsAsync_ShouldReturnListings()
    {
        // Arrange
        var json = @"{ ""listings"": [ { ""globalId"": 3, ""listingUrl"": ""url3"", ""isProject"": true } ] }";
        SetupResponse(json);

        // Act
        var result = await _client.SearchProjectsAsync("utrecht");

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("3", result[0].FundaId);
        Assert.Equal("Nieuwbouwproject", result[0].PropertyType);
    }

    [Fact]
    public async Task SearchAllBuyPagesAsync_PartialFailure_ShouldReturnSuccessResults()
    {
        // Arrange
        var jsonPage1 = @"{ ""listings"": [ { ""globalId"": 101, ""listingUrl"": ""u1"" } ] }";
        var jsonPage3 = @"{ ""listings"": [ { ""globalId"": 102, ""listingUrl"": ""u2"" } ] }";

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
        Assert.Contains(result, l => l.FundaId == "101");
        Assert.Contains(result, l => l.FundaId == "102");
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

    private void SetupHtmlResponse(string html)
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
                Content = new StringContent(html)
            });
    }
}
