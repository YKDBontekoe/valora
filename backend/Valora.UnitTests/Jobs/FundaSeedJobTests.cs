using Microsoft.Extensions.Logging;
using Moq;
using Valora.Application.Scraping;
using Valora.Infrastructure.Jobs;

namespace Valora.UnitTests.Jobs;

public class FundaSeedJobTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldCallScraper()
    {
        // Arrange
        var scraperMock = new Mock<IFundaScraperService>();
        var loggerMock = new Mock<ILogger<FundaSeedJob>>();
        var job = new FundaSeedJob(scraperMock.Object, loggerMock.Object);
        var region = "amsterdam";

        // Act
        await job.ExecuteAsync(region);

        // Assert
        scraperMock.Verify(x => x.ScrapeLimitedAsync(region, 10, It.IsAny<CancellationToken>()), Times.Once);
    }
}
