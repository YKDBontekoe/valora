using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Valora.Application.Common.Interfaces;
using Valora.Application.Scraping;
using Valora.Domain.Entities;
using Valora.Infrastructure.Scraping;

namespace Valora.UnitTests.Scraping;

public class FundaScraperServiceTests
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly Mock<IListingRepository> _listingRepoMock;
    private readonly Mock<IPriceHistoryRepository> _priceHistoryRepoMock;
    private readonly Mock<IScraperNotificationService> _notificationServiceMock;
    private readonly Mock<ILogger<FundaScraperService>> _loggerMock;
    private readonly FundaScraperService _service;

    public FundaScraperServiceTests()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(_httpMessageHandlerMock.Object);

        _listingRepoMock = new Mock<IListingRepository>();
        _priceHistoryRepoMock = new Mock<IPriceHistoryRepository>();
        _notificationServiceMock = new Mock<IScraperNotificationService>();
        _loggerMock = new Mock<ILogger<FundaScraperService>>();

        var options = Options.Create(new ScraperOptions
        {
            SearchUrls = [],
            DelayBetweenRequestsMs = 0 // No delay for tests
        });

        _service = new FundaScraperService(
            httpClient,
            _listingRepoMock.Object,
            _priceHistoryRepoMock.Object,
            options,
            _loggerMock.Object,
            _notificationServiceMock.Object
        );
    }

    [Fact]
    public async Task ScrapeLimitedAsync_ShouldProcessListings_AndSendNotifications()
    {
        // Arrange
        var searchHtml = @"
<html>
    <body>
        <a href=""/detail/koop/amsterdam/huis-12345678-test-straat-1/"">Test straat 1 € 500.000 k.k.</a>
        <a href=""/detail/koop/amsterdam/huis-87654321-test-straat-2/"">Test straat 2 € 600.000 k.k.</a>
    </body>
</html>";

        var detailHtml1 = @"
<html>
    <body>
        <h1>Test straat 1 1234AB Amsterdam</h1>
        <span>€ 500.000 k.k.</span>
        <dt>Wonen</dt><dd>100 m²</dd>
    </body>
</html>";

        var detailHtml2 = @"
<html>
    <body>
        <h1>Test straat 2 1234AB Amsterdam</h1>
        <span>€ 600.000 k.k.</span>
        <dt>Wonen</dt><dd>120 m²</dd>
    </body>
</html>";

        SetupHttpMock("https://www.funda.nl/koop/amsterdam/", searchHtml);
        SetupHttpMock("https://www.funda.nl/detail/koop/amsterdam/huis-12345678-test-straat-1/", detailHtml1);
        SetupHttpMock("https://www.funda.nl/detail/koop/amsterdam/huis-87654321-test-straat-2/", detailHtml2);

        // Act
        await _service.ScrapeLimitedAsync("amsterdam", 2);

        // Assert
        // Verify notifications
        _notificationServiceMock.Verify(x => x.NotifyProgressAsync(It.Is<string>(s => s.Contains("Starting search"))), Times.Once);
        _notificationServiceMock.Verify(x => x.NotifyProgressAsync(It.Is<string>(s => s.Contains("Fetching search results"))), Times.Once);
        _notificationServiceMock.Verify(x => x.NotifyProgressAsync(It.Is<string>(s => s.Contains("Found 2 listings"))), Times.Once);

        // Verify 2 found notifications
        _notificationServiceMock.Verify(x => x.NotifyListingFoundAsync(It.IsAny<string>()), Times.Exactly(2));

        // Verify completion
        _notificationServiceMock.Verify(x => x.NotifyCompleteAsync(), Times.Once);

        // Verify repository calls
        _listingRepoMock.Verify(x => x.AddAsync(It.IsAny<Listing>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ScrapeLimitedAsync_ShouldEnforceLimit()
    {
        // Arrange
        // HTML with 3 listings
        var searchHtml = @"
<html>
    <body>
        <a href=""/detail/koop/amsterdam/huis-101-test/"">1</a>
        <a href=""/detail/koop/amsterdam/huis-102-test/"">2</a>
        <a href=""/detail/koop/amsterdam/huis-103-test/"">3</a>
    </body>
</html>";

        SetupHttpMock("https://www.funda.nl/koop/amsterdam/", searchHtml);
        SetupHttpMock("https://www.funda.nl/detail/koop/amsterdam/huis-101-test/", "<html><h1>1</h1></html>");

        // Act
        // Limit to 1
        await _service.ScrapeLimitedAsync("amsterdam", 1);

        // Assert
        // Should only fetch detail for first listing
        _listingRepoMock.Verify(x => x.AddAsync(It.IsAny<Listing>(), It.IsAny<CancellationToken>()), Times.Once);

        // Should not have fetched 2 or 3 (mock strictness or verify calls)
        // Check that only 1 listing found notification was sent
        _notificationServiceMock.Verify(x => x.NotifyListingFoundAsync(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ScrapeLimitedAsync_ShouldHandleErrors_AndNotify()
    {
        // Arrange
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        // Act
        // The service swallows exceptions from FetchPageAsync and notifies error
        await _service.ScrapeLimitedAsync("amsterdam", 10);

        // Assert
        // Verify error notification
        _notificationServiceMock.Verify(x => x.NotifyErrorAsync(It.IsAny<string>()), Times.Once);
    }

    private void SetupHttpMock(string url, string responseHtml)
    {
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString() == url),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseHtml)
            });
    }
}
