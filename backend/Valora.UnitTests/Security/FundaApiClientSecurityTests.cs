using System.Net;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Valora.Infrastructure.Scraping;

namespace Valora.UnitTests.Security;

public class FundaApiClientSecurityTests
{
    private readonly Mock<HttpMessageHandler> _httpHandlerMock;
    private readonly FundaApiClient _client;
    private readonly Mock<ILogger<FundaApiClient>> _loggerMock;

    public FundaApiClientSecurityTests()
    {
        _httpHandlerMock = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(_httpHandlerMock.Object);
        _loggerMock = new Mock<ILogger<FundaApiClient>>();

        _client = new FundaApiClient(httpClient, _loggerMock.Object);
    }

    [Fact]
    public async Task GetListingDetailsAsync_MaliciousUrl_ReturnsEmptyAndLogsWarning()
    {
        // Arrange
        var maliciousUrl = "https://malicious.com/hack.js";

        // Act
        var result = await _client.GetListingDetailsAsync(maliciousUrl, CancellationToken.None);

        // Assert
        Assert.Null(result); // GetListingDetailsAsync returns null if GetListingDetailHtmlAsync returns empty

        // Verify no HTTP request was made
        _httpHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Never(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Fact]
    public async Task GetListingDetailsAsync_NonHttpsUrl_ReturnsEmpty()
    {
        // Arrange
        var insecureUrl = "http://www.funda.nl/listing";

        // Act
        var result = await _client.GetListingDetailsAsync(insecureUrl, CancellationToken.None);

        // Assert
        Assert.Null(result);

        // Verify no HTTP request was made
        _httpHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Never(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Fact]
    public async Task GetListingDetailsAsync_ValidFundaUrl_MakesRequest()
    {
        // Arrange
        var validUrl = "https://www.funda.nl/koop/amsterdam/huis-123";
        var html = "<html></html>";

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
        await _client.GetListingDetailsAsync(validUrl, CancellationToken.None);

        // Assert
        _httpHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString() == validUrl),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Theory]
    [InlineData("1234AB")]
    [InlineData("1234 AB")]
    public async Task GetFiberAvailabilityAsync_ValidPostalCode_MakesRequest(string postalCode)
    {
        // Arrange
        var json = @"{ ""postalCode"": ""1234AB"", ""availability"": true }";

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
                Content = new StringContent(json)
            });

        // Act
        var result = await _client.GetFiberAvailabilityAsync(postalCode);

        // Assert
        Assert.NotNull(result);
        _httpHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("1234AB")),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Theory]
    [InlineData("1234/..")]
    [InlineData("../../../etc/passwd")]
    [InlineData("12A4AB")] // Invalid digits
    [InlineData("1234A")] // Too short
    [InlineData("1234ABC")] // Too long
    public async Task GetFiberAvailabilityAsync_InvalidPostalCode_ReturnsNullAndNoRequest(string postalCode)
    {
        // Act
        var result = await _client.GetFiberAvailabilityAsync(postalCode);

        // Assert
        Assert.Null(result);

        // Verify no HTTP request was made
        _httpHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Never(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>()
        );
    }
}
