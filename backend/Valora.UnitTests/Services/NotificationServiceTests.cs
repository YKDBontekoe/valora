using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using Moq;
using Valora.Application.Services;
using Valora.Infrastructure.Persistence.Repositories;
using Xunit;

namespace Valora.UnitTests.Services;

public class NotificationServiceTests
{
    private readonly ValoraDbContext _context;
    private readonly NotificationService _service;
    private readonly Mock<ILogger<NotificationService>> _mockLogger;
    private readonly TimeProvider _timeProvider;

    public NotificationServiceTests()
    {
        var options = new DbContextOptionsBuilder<ValoraDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ValoraDbContext(options);
        _mockLogger = new Mock<ILogger<NotificationService>>();
        _timeProvider = TimeProvider.System;

        var repository = new NotificationRepository(_context);
        _service = new NotificationService(repository, _mockLogger.Object, _timeProvider);
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
}
