using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Valora.Infrastructure.Scraping;
using Valora.Infrastructure.Scraping.Models;

namespace Valora.UnitTests.Scraping;

public class FundaApiClientTests
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly Mock<ILogger<FundaApiClient>> _loggerMock;
    private readonly FundaApiClient _client;

    public FundaApiClientTests()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _loggerMock = new Mock<ILogger<FundaApiClient>>();

        var httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri("https://search-topposition.funda.io/")
        };

        _client = new FundaApiClient(httpClient, _loggerMock.Object);
    }

    [Fact]
    public async Task SearchBuyAsync_InvalidJson_ShouldThrowHttpRequestException()
    {
        // Arrange
        var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("Invalid JSON content")
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(response);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            _client.SearchBuyAsync("amsterdam"));
    }

    [Fact]
    public async Task SearchAllBuyPagesAsync_PartialFailure_ShouldReturnResultsFromSuccessfulPages()
    {
        // Arrange
        // Page 1 success
        var page1Content = JsonSerializer.Serialize(new FundaApiResponse
        {
            Listings = [new FundaApiListing { GlobalId = 1, ListingUrl = "u1" }]
        });

        // Page 2 failure (simulated by throwing in SendAsync or returning invalid JSON)
        // Let's use invalid JSON for page 2 to trigger the catch block in SearchAsync, which throws,
        // which is then caught by AggregatePagesAsync.

        _httpMessageHandlerMock
            .Protected()
            .SetupSequence<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(page1Content)
            })
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("Invalid JSON")
            });

        // Act
        var results = await _client.SearchAllBuyPagesAsync("amsterdam", maxPages: 2);

        // Assert
        Assert.Single(results);
        Assert.Equal(1, results[0].GlobalId);

        // Verify logger was called for the failure
        // We expect LogWarning from AggregatePagesAsync or LogError from SearchAsync?
        // SearchAsync logs Error then throws. AggregatePagesAsync catches and logs Warning.
        // So we should see LogWarning.
    }
}
