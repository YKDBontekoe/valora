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
}
