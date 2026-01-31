using Microsoft.Extensions.Logging;
using Moq;
using Valora.Application.Scraping;
using Valora.Infrastructure.Jobs;

namespace Valora.UnitTests.Jobs;

public class FundaScraperJobTests
{
    [Fact]
    public async Task ExecuteLimitedAsync_ShouldCallScraperWithCorrectParams()
    {
        // Arrange
        var scraperMock = new Mock<IFundaScraperService>();
        var loggerMock = new Mock<ILogger<FundaScraperJob>>();
        var job = new FundaScraperJob(scraperMock.Object, loggerMock.Object);
        var region = "rotterdam";
        var limit = 5;

        // Act
        await job.ExecuteLimitedAsync(region, limit);

        // Assert
        scraperMock.Verify(x => x.ScrapeLimitedAsync(region, limit, It.IsAny<CancellationToken>()), Times.Once);
    }
}
