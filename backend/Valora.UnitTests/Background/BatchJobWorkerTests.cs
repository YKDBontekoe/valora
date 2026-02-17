using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Valora.Api.Background;
using Valora.Application.Common.Interfaces;

namespace Valora.UnitTests.Background;

public class BatchJobWorkerTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldProcessJobs()
    {
        // Arrange
        var serviceProviderMock = new Mock<IServiceProvider>();
        var serviceScopeMock = new Mock<IServiceScope>();
        var serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();
        var jobServiceMock = new Mock<IBatchJobService>();
        var loggerMock = new Mock<ILogger<BatchJobWorker>>();

        serviceProviderMock.Setup(x => x.GetService(typeof(IServiceScopeFactory)))
            .Returns(serviceScopeFactoryMock.Object);
        serviceScopeFactoryMock.Setup(x => x.CreateScope())
            .Returns(serviceScopeMock.Object);
        serviceScopeMock.Setup(x => x.ServiceProvider)
            .Returns(serviceProviderMock.Object);
        serviceProviderMock.Setup(x => x.GetService(typeof(IBatchJobService)))
            .Returns(jobServiceMock.Object);

        var worker = new BatchJobWorker(serviceProviderMock.Object, loggerMock.Object);
        var cts = new CancellationTokenSource();

        // Act
        var task = worker.StartAsync(cts.Token);

        await Task.Delay(100);
        cts.Cancel();
        await task;

        // Assert
        jobServiceMock.Verify(x => x.ProcessNextJobAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldHandleExceptionInService()
    {
        // Arrange
        var serviceProviderMock = new Mock<IServiceProvider>();
        var serviceScopeMock = new Mock<IServiceScope>();
        var serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();
        var jobServiceMock = new Mock<IBatchJobService>();
        var loggerMock = new Mock<ILogger<BatchJobWorker>>();

        serviceProviderMock.Setup(x => x.GetService(typeof(IServiceScopeFactory)))
            .Returns(serviceScopeFactoryMock.Object);
        serviceScopeFactoryMock.Setup(x => x.CreateScope())
            .Returns(serviceScopeMock.Object);
        serviceScopeMock.Setup(x => x.ServiceProvider)
            .Returns(serviceProviderMock.Object);
        serviceProviderMock.Setup(x => x.GetService(typeof(IBatchJobService)))
            .Returns(jobServiceMock.Object);

        jobServiceMock.Setup(x => x.ProcessNextJobAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Worker Error"));

        var worker = new BatchJobWorker(serviceProviderMock.Object, loggerMock.Object);
        var cts = new CancellationTokenSource();

        // Act
        var task = worker.StartAsync(cts.Token);

        await Task.Delay(100);
        cts.Cancel();
        await task;

        // Assert
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error occurred while processing batch jobs.")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }
}
