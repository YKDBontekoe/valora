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
    public async Task ScrapeAndStoreAsync_ShouldProcessAllUrls()
    {
        // Arrange
        // Re-create service with multiple URLs
        var options = Options.Create(new ScraperOptions
        {
            SearchUrls = ["http://url1", "http://url2"],
            DelayBetweenRequestsMs = 0
        });

        var service = new FundaScraperService(
            new HttpClient(_httpMessageHandlerMock.Object),
            _listingRepoMock.Object,
            _priceHistoryRepoMock.Object,
            options,
            _loggerMock.Object,
            _notificationServiceMock.Object
        );

        SetupHttpMock("http://url1/", "<html><a href='/detail/koop/city/huis-1-street/'>Link 1</a></html>");
        SetupHttpMock("http://url2/", "<html><a href='/detail/koop/city/huis-2-street/'>Link 2</a></html>");
        SetupHttpMock("https://www.funda.nl/detail/koop/city/huis-1-street/", "<html><h1>Addr1</h1></html>");
        SetupHttpMock("https://www.funda.nl/detail/koop/city/huis-2-street/", "<html><h1>Addr2</h1></html>");

        // Mock GetByFundaIdAsync to return null (new listings)
        _listingRepoMock.Setup(x => x.GetByFundaIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Listing?)null);

        // Act
        await service.ScrapeAndStoreAsync();

        // Assert
        // Should verify AddAsync called 2 times (once per URL finding 1 listing)
        _listingRepoMock.Verify(x => x.AddAsync(It.IsAny<Listing>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ScrapeAndStoreAsync_ShouldUpdateExistingListings()
    {
        // Arrange
        var searchHtml = "<html><a href='/detail/koop/city/huis-1-street/'>€ 600.000</a></html>";
        var detailHtml = "<html><h1>Addr1</h1><span>€ 600.000</span></html>";

        SetupHttpMock("https://www.funda.nl/koop/amsterdam/", searchHtml);
        SetupHttpMock("https://www.funda.nl/detail/koop/city/huis-1-street/", detailHtml);

        var existingListing = new Listing
        {
            Id = Guid.NewGuid(),
            FundaId = "1",
            Price = 500000, // Old price
            Address = "Addr1"
        };

        _listingRepoMock.Setup(x => x.GetByFundaIdAsync("1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingListing);

        // Options need a URL
        var options = Options.Create(new ScraperOptions
        {
            SearchUrls = ["https://www.funda.nl/koop/amsterdam/"],
            DelayBetweenRequestsMs = 0
        });

        // Ensure parser finds the correct URL and mock is ready for it
        // The parser logic: href -> startsWith http ? href : "https://www.funda.nl" + href
        // href is /detail/koop/city/1/ -> fullUrl is https://www.funda.nl/detail/koop/city/1/

        var service = new FundaScraperService(
            new HttpClient(_httpMessageHandlerMock.Object),
            _listingRepoMock.Object,
            _priceHistoryRepoMock.Object,
            options,
            _loggerMock.Object,
            _notificationServiceMock.Object
        );

        // Act
        await service.ScrapeAndStoreAsync();

        // Assert
        // Should update listing
        _listingRepoMock.Verify(x => x.UpdateAsync(It.Is<Listing>(l => l.Price == 600000), It.IsAny<CancellationToken>()), Times.Once);

        // Should add price history
        _priceHistoryRepoMock.Verify(x => x.AddAsync(It.Is<PriceHistory>(ph => ph.Price == 600000 && ph.ListingId == existingListing.Id), It.IsAny<CancellationToken>()), Times.Once);
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

    [Fact]
    public async Task ProcessListingAsync_ShouldContinue_WhenParsingFails()
    {
        // Arrange
        var searchHtml = "<html><a href='/detail/koop/city/huis-1-street/'>1</a></html>";
        // Invalid detail HTML that fails parsing (no h1)
        var detailHtml = "<html><body>No address here</body></html>";

        SetupHttpMock("https://www.funda.nl/koop/amsterdam/", searchHtml);
        SetupHttpMock("https://www.funda.nl/detail/koop/city/huis-1-street/", detailHtml);

        // Act
        await _service.ScrapeLimitedAsync("amsterdam", 1);

        // Assert
        // ParseListingDetail should return null, causing ProcessListingAsync to return early
        _listingRepoMock.Verify(x => x.AddAsync(It.IsAny<Listing>(), It.IsAny<CancellationToken>()), Times.Never);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Failed to parse listing")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateExistingListingAsync_ShouldNotAddPriceHistory_WhenPriceIsSame()
    {
        // Arrange
        var searchHtml = "<html><a href='/detail/koop/city/huis-1-street/'>€ 500.000</a></html>";
        var detailHtml = "<html><h1>Addr1</h1><span>€ 500.000</span></html>";

        SetupHttpMock("https://www.funda.nl/koop/amsterdam/", searchHtml);
        SetupHttpMock("https://www.funda.nl/detail/koop/city/huis-1-street/", detailHtml);

        var existingListing = new Listing
        {
            Id = Guid.NewGuid(),
            FundaId = "1",
            Price = 500000,
            Address = "Addr1"
        };

        _listingRepoMock.Setup(x => x.GetByFundaIdAsync("1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingListing);

        // Act
        await _service.ScrapeLimitedAsync("amsterdam", 1);

        // Assert
        // Update called
        _listingRepoMock.Verify(x => x.UpdateAsync(It.IsAny<Listing>(), It.IsAny<CancellationToken>()), Times.Once);
        // Price history NOT added
        _priceHistoryRepoMock.Verify(x => x.AddAsync(It.IsAny<PriceHistory>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddNewListingAsync_ShouldSwallowNotificationException()
    {
        // Arrange
        var searchHtml = "<html><a href='/detail/koop/city/huis-1-street/'>1</a></html>";
        var detailHtml = "<html><h1>Addr1</h1></html>";

        SetupHttpMock("https://www.funda.nl/koop/amsterdam/", searchHtml);
        SetupHttpMock("https://www.funda.nl/detail/koop/city/huis-1-street/", detailHtml);

        _notificationServiceMock.Setup(x => x.NotifyListingFoundAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Notification failed"));

        // Act
        // Should not throw
        await _service.ScrapeLimitedAsync("amsterdam", 1);

        // Assert
        _listingRepoMock.Verify(x => x.AddAsync(It.IsAny<Listing>(), It.IsAny<CancellationToken>()), Times.Once);
        // Verify we logged the warning
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Failed to send notification")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    private void SetupHttpMock(string url, string responseHtml)
    {
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.AbsoluteUri == url),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseHtml)
            });
    }
}
