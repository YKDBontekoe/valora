using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Valora.Application.Common.Events;
using Valora.Application.Common.Interfaces;
using Valora.Infrastructure.Services;
using Xunit;

namespace Valora.UnitTests.Services;

public class EventDispatcherTests
{
    private readonly Mock<ILogger<EventDispatcher>> _loggerMock = new();
    private readonly Mock<IServiceProvider> _serviceProviderMock = new();

    public record TestEvent : IDomainEvent;

    public class TestEventHandler : IEventHandler<TestEvent>
    {
        public bool Handled { get; private set; }
        public Task HandleAsync(TestEvent domainEvent, CancellationToken cancellationToken = default)
        {
            Handled = true;
            return Task.CompletedTask;
        }
    }

    public class FailingEventHandler : IEventHandler<TestEvent>
    {
        public Task HandleAsync(TestEvent domainEvent, CancellationToken cancellationToken = default)
        {
            throw new Exception("Handler failed");
        }
    }

    [Fact]
    public async Task DispatchAsync_ShouldCallAllRegisteredHandlers()
    {
        // Arrange
        var handler1 = new TestEventHandler();
        var handler2 = new TestEventHandler();

        var services = new ServiceCollection();
        services.AddSingleton<IEventHandler<TestEvent>>(handler1);
        services.AddSingleton<IEventHandler<TestEvent>>(handler2);

        var provider = services.BuildServiceProvider();
        var dispatcher = new EventDispatcher(provider, _loggerMock.Object);

        // Act
        await dispatcher.DispatchAsync(new TestEvent());

        // Assert
        Assert.True(handler1.Handled);
        Assert.True(handler2.Handled);
    }

    [Fact]
    public async Task DispatchAsync_ShouldCatchExceptionsAndContinue()
    {
        // Arrange
        var failingHandler = new FailingEventHandler();
        var successHandler = new TestEventHandler();

        var services = new ServiceCollection();
        services.AddSingleton<IEventHandler<TestEvent>>(failingHandler);
        services.AddSingleton<IEventHandler<TestEvent>>(successHandler);

        var provider = services.BuildServiceProvider();
        var dispatcher = new EventDispatcher(provider, _loggerMock.Object);

        // Act
        await dispatcher.DispatchAsync(new TestEvent());

        // Assert
        Assert.True(successHandler.Handled);
    }
}
