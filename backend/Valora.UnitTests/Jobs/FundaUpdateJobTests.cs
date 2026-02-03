using Microsoft.Extensions.Logging;
using Moq;
using Valora.Application.Scraping;
using Valora.Infrastructure.Jobs;
using Xunit;

namespace Valora.UnitTests.Jobs;

public class FundaUpdateJobTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldCallUpdateExistingListingsAsync()
    {
        // Arrange
        var scraperMock = new Mock<IFundaScraperService>();
        var loggerMock = new Mock<ILogger<FundaUpdateJob>>();
        var job = new FundaUpdateJob(scraperMock.Object, loggerMock.Object);

        // Act
        await job.ExecuteAsync(CancellationToken.None);

        // Assert
        scraperMock.Verify(x => x.UpdateExistingListingsAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldLogAndRethrow_WhenServiceFails()
    {
        // Arrange
        var scraperMock = new Mock<IFundaScraperService>();
        scraperMock.Setup(x => x.UpdateExistingListingsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Scraper error"));

        var loggerMock = new Mock<ILogger<FundaUpdateJob>>();
        var job = new FundaUpdateJob(scraperMock.Object, loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => job.ExecuteAsync(CancellationToken.None));
    }
}
