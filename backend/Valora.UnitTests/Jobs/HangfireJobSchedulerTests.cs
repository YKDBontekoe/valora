using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Moq;
using Valora.Infrastructure.Jobs;
using Xunit;

namespace Valora.UnitTests.Jobs;

public class HangfireJobSchedulerTests
{
    private readonly Mock<IBackgroundJobClient> _mockClient;
    private readonly HangfireJobScheduler _scheduler;

    public HangfireJobSchedulerTests()
    {
        _mockClient = new Mock<IBackgroundJobClient>();
        _scheduler = new HangfireJobScheduler(_mockClient.Object);
    }

    [Fact]
    public async Task EnqueueScraperJobAsync_ShouldEnqueueJob()
    {
        // Act
        await _scheduler.EnqueueScraperJobAsync(CancellationToken.None);

        // Assert
        _mockClient.Verify(x => x.Create(
            It.Is<Job>(job => job.Type == typeof(FundaScraperJob) && job.Method.Name == "ExecuteAsync"),
            It.IsAny<EnqueuedState>()));
    }

    [Fact]
    public async Task EnqueueSeedJobAsync_ShouldEnqueueJob_WithRegion()
    {
        // Act
        await _scheduler.EnqueueSeedJobAsync("Amsterdam", CancellationToken.None);

        // Assert
        _mockClient.Verify(x => x.Create(
            It.Is<Job>(job =>
                job.Type == typeof(FundaSeedJob) &&
                job.Method.Name == "ExecuteAsync" &&
                (string)job.Args[0] == "Amsterdam"),
            It.IsAny<EnqueuedState>()));
    }
}
