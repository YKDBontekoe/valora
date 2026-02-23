using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Valora.Api.Background;
using Valora.Application.Common.Interfaces;

namespace Valora.UnitTests.Background;

public class BatchJobWorkerTests
{
    private class TestBatchJobWorker : BatchJobWorker
    {
        public TestBatchJobWorker(IServiceProvider serviceProvider, ILogger<BatchJobWorker> logger)
            : base(serviceProvider, logger) { }

        public Task PublicExecuteAsync(CancellationToken stoppingToken) => ExecuteAsync(stoppingToken);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldProcessJobs()
    {
        // Arrange
        var serviceProviderMock = new Mock<IServiceProvider>();
        var serviceScopeMock = new Mock<IServiceScope>();
        var serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();
        var jobExecutorMock = new Mock<IBatchJobExecutor>();
        var loggerMock = new Mock<ILogger<BatchJobWorker>>();

        serviceProviderMock.Setup(x => x.GetService(typeof(IServiceScopeFactory)))
            .Returns(serviceScopeFactoryMock.Object);
        serviceScopeFactoryMock.Setup(x => x.CreateScope())
            .Returns(serviceScopeMock.Object);
        serviceScopeMock.Setup(x => x.ServiceProvider)
            .Returns(serviceProviderMock.Object);
        serviceProviderMock.Setup(x => x.GetService(typeof(IBatchJobExecutor)))
            .Returns(jobExecutorMock.Object);

        var worker = new TestBatchJobWorker(serviceProviderMock.Object, loggerMock.Object);
        var cts = new CancellationTokenSource();

        // Act
        var task = worker.PublicExecuteAsync(cts.Token);

        // Wait for it to start and do one iteration
        await Task.Delay(500);
        cts.Cancel();
        try { await task; } catch (OperationCanceledException) { }

        // Assert
        jobExecutorMock.Verify(x => x.ProcessNextJobAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldHandleExceptionInService()
    {
        // Arrange
        var serviceProviderMock = new Mock<IServiceProvider>();
        var serviceScopeMock = new Mock<IServiceScope>();
        var serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();
        var jobExecutorMock = new Mock<IBatchJobExecutor>();
        var loggerMock = new Mock<ILogger<BatchJobWorker>>();

        serviceProviderMock.Setup(x => x.GetService(typeof(IServiceScopeFactory)))
            .Returns(serviceScopeFactoryMock.Object);
        serviceScopeFactoryMock.Setup(x => x.CreateScope())
            .Returns(serviceScopeMock.Object);
        serviceScopeMock.Setup(x => x.ServiceProvider)
            .Returns(serviceProviderMock.Object);
        serviceProviderMock.Setup(x => x.GetService(typeof(IBatchJobExecutor)))
            .Returns(jobExecutorMock.Object);

        jobExecutorMock.Setup(x => x.ProcessNextJobAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Worker Error"));

        var worker = new TestBatchJobWorker(serviceProviderMock.Object, loggerMock.Object);
        var cts = new CancellationTokenSource();

        // Act
        var task = worker.PublicExecuteAsync(cts.Token);

        await Task.Delay(500);
        cts.Cancel();
        try { await task; } catch (OperationCanceledException) { }

        // Assert
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldStopWorkerOnDatabaseAuthenticationFailure()
    {
        // Arrange
        var serviceProviderMock = new Mock<IServiceProvider>();
        var serviceScopeMock = new Mock<IServiceScope>();
        var serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();
        var jobExecutorMock = new Mock<IBatchJobExecutor>();
        var loggerMock = new Mock<ILogger<BatchJobWorker>>();

        serviceProviderMock.Setup(x => x.GetService(typeof(IServiceScopeFactory)))
            .Returns(serviceScopeFactoryMock.Object);
        serviceScopeFactoryMock.Setup(x => x.CreateScope())
            .Returns(serviceScopeMock.Object);
        serviceScopeMock.Setup(x => x.ServiceProvider)
            .Returns(serviceProviderMock.Object);
        serviceProviderMock.Setup(x => x.GetService(typeof(IBatchJobExecutor)))
            .Returns(jobExecutorMock.Object);

        jobExecutorMock.Setup(x => x.ProcessNextJobAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Login failed for user 'ykdbonte'."));

        var worker = new TestBatchJobWorker(serviceProviderMock.Object, loggerMock.Object);

        // Act
        await worker.PublicExecuteAsync(CancellationToken.None);

        // Assert
        jobExecutorMock.Verify(x => x.ProcessNextJobAsync(It.IsAny<CancellationToken>()), Times.Once);
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Critical,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
