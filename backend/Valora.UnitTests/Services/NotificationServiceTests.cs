using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Valora.Application.Services;
using Valora.Application.Common.Interfaces;
using Valora.Infrastructure.Persistence.Repositories;
using Xunit;

namespace Valora.UnitTests.Services;

public class NotificationServiceTests
{
    private readonly ValoraDbContext _context;
    private readonly NotificationService _service;
    private readonly Mock<ILogger<NotificationService>> _mockLogger;
    private readonly Mock<INotificationPublisher> _mockPublisher;
    private readonly FakeTimeProvider _timeProvider;

    public NotificationServiceTests()
    {
        var options = new DbContextOptionsBuilder<ValoraDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ValoraDbContext(options);
        _mockLogger = new Mock<ILogger<NotificationService>>();
        _mockPublisher = new Mock<INotificationPublisher>();

        // Setup FakeTimeProvider with a fixed time
        _timeProvider = new FakeTimeProvider();
        _timeProvider.SetUtcNow(new DateTimeOffset(2023, 10, 1, 12, 0, 0, TimeSpan.Zero));

        // Note: NotificationRepository also needs TimeProvider now
        var repository = new NotificationRepository(_context, _timeProvider);
        _service = new NotificationService(repository, _mockLogger.Object, _timeProvider, _mockPublisher.Object);
    }

    [Fact]
    public async Task DeleteNotificationAsync_ShouldReturnTrue_WhenNotificationExists()
    {
        // Arrange
        var userId = "user1";
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = "Test",
            Body = "Body",
            Type = NotificationType.Info
        };
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.DeleteNotificationAsync(notification.Id, userId);

        // Assert
        Assert.True(result);
        var deleted = await _context.Notifications.FindAsync(notification.Id);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteNotificationAsync_ShouldReturnFalse_WhenNotificationDoesNotExist()
    {
        // Act
        var result = await _service.DeleteNotificationAsync(Guid.NewGuid(), "user1");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteNotificationAsync_ShouldReturnFalse_WhenNotificationBelongsToDifferentUser()
    {
        // Arrange
        var userId = "user1";
        var otherUserId = "user2";
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = otherUserId,
            Title = "Test",
            Body = "Body",
            Type = NotificationType.Info
        };
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.DeleteNotificationAsync(notification.Id, userId);

        // Assert
        Assert.False(result);
        var existing = await _context.Notifications.FindAsync(notification.Id);
        Assert.NotNull(existing);
    }

    [Fact]
    public async Task CreateNotificationAsync_ShouldUseTimeProviderForCreatedAt()
    {
        // Arrange
        var userId = "user1";
        var title = "Test Notification";
        var body = "This is a test.";

        // Act
        await _service.CreateNotificationAsync(userId, title, body, NotificationType.Info);

        // Assert
        var notification = await _context.Notifications.SingleAsync();
        Assert.Equal(_timeProvider.GetUtcNow().UtcDateTime, notification.CreatedAt);
    }

    [Fact]
    public async Task MarkAllAsReadAsync_ShouldUpdateUpdatedAtTimestamp()
    {
        // Arrange
        var userId = "user1";
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = "Test",
            Body = "Body",
            Type = NotificationType.Info,
            IsRead = false,
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime.AddHours(-1)
        };
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        // Advance time
        _timeProvider.Advance(TimeSpan.FromHours(1));
        var expectedUpdateTime = _timeProvider.GetUtcNow().UtcDateTime;

        // Act
        await _service.MarkAllAsReadAsync(userId);

        // Assert
        var updatedNotification = await _context.Notifications.FindAsync(notification.Id);
        Assert.True(updatedNotification!.IsRead);
        Assert.Equal(expectedUpdateTime, updatedNotification.UpdatedAt);
    }
}
