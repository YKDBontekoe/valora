using Microsoft.EntityFrameworkCore;
using Valora.Infrastructure.Persistence;
using Valora.Infrastructure.Persistence.Repositories;
using Valora.Domain.Entities;
using Xunit;
using Moq;

namespace Valora.UnitTests.Repositories;

public class NotificationRepositoryTests
{
    private readonly DbContextOptions<ValoraDbContext> _options;
    private readonly Mock<TimeProvider> _mockTimeProvider;

    public NotificationRepositoryTests()
    {
        _options = new DbContextOptionsBuilder<ValoraDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _mockTimeProvider = new Mock<TimeProvider>();
        _mockTimeProvider.Setup(x => x.GetUtcNow()).Returns(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task CountAsync_ShouldReturnTotalCount()
    {
        // Arrange
        using var context = new ValoraDbContext(_options);
        context.Notifications.AddRange(
            new Notification { UserId = "user1", Title = "T1", Body = "B1", CreatedAt = DateTime.UtcNow },
            new Notification { UserId = "user2", Title = "T2", Body = "B2", CreatedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var repository = new NotificationRepository(context, _mockTimeProvider.Object);

        // Act
        var count = await repository.CountAsync();

        // Assert
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task GetUnreadCountAsync_ShouldReturnOnlyUnread()
    {
        // Arrange
        using var context = new ValoraDbContext(_options);
        context.Notifications.AddRange(
            new Notification { UserId = "user1", IsRead = false, CreatedAt = DateTime.UtcNow },
            new Notification { UserId = "user1", IsRead = true, CreatedAt = DateTime.UtcNow },
            new Notification { UserId = "user2", IsRead = false, CreatedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var repository = new NotificationRepository(context, _mockTimeProvider.Object);

        // Act
        var count = await repository.GetUnreadCountAsync("user1");

        // Assert
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task MarkAllAsReadAsync_ShouldUpdateAllUnread()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        _mockTimeProvider.Setup(x => x.GetUtcNow()).Returns(now);

        using var context = new ValoraDbContext(_options);
        context.Notifications.AddRange(
            new Notification { UserId = "user1", IsRead = false, CreatedAt = DateTime.UtcNow },
            new Notification { UserId = "user1", IsRead = false, CreatedAt = DateTime.UtcNow },
            new Notification { UserId = "user2", IsRead = false, CreatedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var repository = new NotificationRepository(context, _mockTimeProvider.Object);

        // Act
        await repository.MarkAllAsReadAsync("user1");

        // Assert
        var unread = await context.Notifications.Where(n => n.UserId == "user1" && !n.IsRead).CountAsync();
        Assert.Equal(0, unread);

        var otherUnread = await context.Notifications.Where(n => n.UserId == "user2" && !n.IsRead).CountAsync();
        Assert.Equal(1, otherUnread);
    }
}
