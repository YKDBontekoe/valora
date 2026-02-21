using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Moq;
using Valora.Api.Services;
using Valora.Api.Hubs;
using Valora.Application.DTOs;
using Valora.Domain.Entities;
using Xunit;

namespace Valora.UnitTests.Services;

public class SignalRNotificationPublisherTests
{
    private readonly Mock<IHubContext<NotificationHub>> _mockHubContext;
    private readonly Mock<IHubClients> _mockClients;
    private readonly Mock<IClientProxy> _mockClientProxy;
    private readonly SignalRNotificationPublisher _publisher;

    public SignalRNotificationPublisherTests()
    {
        _mockHubContext = new Mock<IHubContext<NotificationHub>>();
        _mockClients = new Mock<IHubClients>();
        _mockClientProxy = new Mock<IClientProxy>();

        _mockHubContext.Setup(x => x.Clients).Returns(_mockClients.Object);
        _mockClients.Setup(x => x.Group(It.IsAny<string>())).Returns(_mockClientProxy.Object);

        _publisher = new SignalRNotificationPublisher(_mockHubContext.Object);
    }

    [Fact]
    public async Task PublishNotificationCreatedAsync_SendsToUserGroup()
    {
        // Arrange
        var userId = "user1";
        var notification = new NotificationDto { Id = Guid.NewGuid(), Title = "Test" };

        // Act
        await _publisher.PublishNotificationCreatedAsync(userId, notification);

        // Assert
        _mockClients.Verify(x => x.Group($"User-{userId}"), Times.Once);
        _mockClientProxy.Verify(
            x => x.SendCoreAsync(
                "NotificationCreated",
                It.Is<object[]>(o => o.Length == 1 && o[0] == notification),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PublishNotificationReadAsync_SendsToUserGroup()
    {
        // Arrange
        var userId = "user1";
        var notificationId = Guid.NewGuid();

        // Act
        await _publisher.PublishNotificationReadAsync(userId, notificationId);

        // Assert
        _mockClients.Verify(x => x.Group($"User-{userId}"), Times.Once);
        _mockClientProxy.Verify(
            x => x.SendCoreAsync(
                "NotificationRead",
                It.Is<object[]>(o => o.Length == 1 && (Guid)o[0] == notificationId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PublishNotificationDeletedAsync_SendsToUserGroup()
    {
        // Arrange
        var userId = "user1";
        var notificationId = Guid.NewGuid();

        // Act
        await _publisher.PublishNotificationDeletedAsync(userId, notificationId);

        // Assert
        _mockClients.Verify(x => x.Group($"User-{userId}"), Times.Once);
        _mockClientProxy.Verify(
            x => x.SendCoreAsync(
                "NotificationDeleted",
                It.Is<object[]>(o => o.Length == 1 && (Guid)o[0] == notificationId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
