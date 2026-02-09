using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Valora.Application.DTOs;
using Valora.Domain.Entities;
using Xunit;

namespace Valora.IntegrationTests;

public class NotificationEndpointTests : BaseIntegrationTest
{
    public NotificationEndpointTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task GetNotifications_ReturnsEmptyList_WhenNoNotificationsExist()
    {
        // Arrange
        await AuthenticateAsync();

        // Act
        var response = await Client.GetAsync("/api/notifications");

        // Assert
        response.EnsureSuccessStatusCode();
        var notifications = await response.Content.ReadFromJsonAsync<List<NotificationDto>>();
        Assert.NotNull(notifications);
        Assert.Empty(notifications);
    }

    [Fact]
    public async Task GetNotifications_ReturnsOnlyUserNotifications()
    {
        // Arrange
        var user1Email = "user1@example.com";
        var user2Email = "user2@example.com";

        // Register and login as User 1
        await AuthenticateAsync(user1Email);
        var user1 = await DbContext.Users.FirstAsync(u => u.Email == user1Email);

        // Register and login as User 2 (this switches current auth token to User 2)
        await AuthenticateAsync(user2Email);
        var user2 = await DbContext.Users.FirstAsync(u => u.Email == user2Email);

        // Add notifications for both users
        DbContext.Notifications.AddRange(
            new Notification { UserId = user1.Id, Title = "User 1 Notif", Body = "Body", Type = NotificationType.Info },
            new Notification { UserId = user2.Id, Title = "User 2 Notif", Body = "Body", Type = NotificationType.Info }
        );
        await DbContext.SaveChangesAsync();

        // Act - Request as User 2 (currently authenticated)
        var response = await Client.GetAsync("/api/notifications");

        // Assert
        response.EnsureSuccessStatusCode();
        var notifications = await response.Content.ReadFromJsonAsync<List<NotificationDto>>();
        Assert.NotNull(notifications);
        Assert.Single(notifications);
        Assert.Equal("User 2 Notif", notifications[0].Title);
    }

    [Fact]
    public async Task GetNotifications_RespectsLimit()
    {
        // Arrange
        var email = "limit@example.com";
        await AuthenticateAsync(email);
        var user = await DbContext.Users.FirstAsync(u => u.Email == email);

        var notifications = Enumerable.Range(1, 10)
            .Select(i => new Notification
            {
                UserId = user.Id,
                Title = $"Notif {i}",
                Body = "Body",
                Type = NotificationType.Info,
                CreatedAt = DateTime.UtcNow.AddMinutes(i) // Ensure order
            })
            .ToList();

        DbContext.Notifications.AddRange(notifications);
        await DbContext.SaveChangesAsync();

        // Act
        var limit = 5;
        var response = await Client.GetAsync($"/api/notifications?limit={limit}");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<List<NotificationDto>>();
        Assert.NotNull(result);
        Assert.Equal(limit, result.Count);
    }

    [Fact]
    public async Task GetNotifications_WithUnreadFilter_ReturnsOnlyUnread()
    {
        // Arrange
        var email = "unreadfilter@example.com";
        await AuthenticateAsync(email);
        var user = await DbContext.Users.FirstAsync(u => u.Email == email);

        DbContext.Notifications.AddRange(
            new Notification { UserId = user.Id, Title = "Read", Body = "Body", Type = NotificationType.Info, IsRead = true },
            new Notification { UserId = user.Id, Title = "Unread", Body = "Body", Type = NotificationType.Info, IsRead = false }
        );
        await DbContext.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync("/api/notifications?unreadOnly=true");

        // Assert
        response.EnsureSuccessStatusCode();
        var notifications = await response.Content.ReadFromJsonAsync<List<NotificationDto>>();
        Assert.NotNull(notifications);
        Assert.Single(notifications);
        Assert.Equal("Unread", notifications[0].Title);
        Assert.False(notifications[0].IsRead);
    }

    [Fact]
    public async Task GetUnreadCount_ReturnsCorrectCount()
    {
        // Arrange
        var email = "unreadcount@example.com";
        await AuthenticateAsync(email);
        var user = await DbContext.Users.FirstAsync(u => u.Email == email);

        DbContext.Notifications.AddRange(
            new Notification { UserId = user.Id, Title = "Read", Body = "Body", Type = NotificationType.Info, IsRead = true },
            new Notification { UserId = user.Id, Title = "Unread 1", Body = "Body", Type = NotificationType.Info, IsRead = false },
            new Notification { UserId = user.Id, Title = "Unread 2", Body = "Body", Type = NotificationType.Info, IsRead = false }
        );
        await DbContext.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync("/api/notifications/unread-count");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, int>>();
        Assert.NotNull(result);
        Assert.True(result.ContainsKey("count"));
        Assert.Equal(2, result["count"]);
    }

    [Fact]
    public async Task MarkAsRead_UpdatesNotificationStatus()
    {
        // Arrange
        var email = "markread@example.com";
        await AuthenticateAsync(email);
        var user = await DbContext.Users.FirstAsync(u => u.Email == email);

        var notification = new Notification
        {
            UserId = user.Id,
            Title = "Unread",
            Body = "Body",
            Type = NotificationType.Info,
            IsRead = false
        };
        DbContext.Notifications.Add(notification);
        await DbContext.SaveChangesAsync();

        // Act
        var response = await Client.PostAsync($"/api/notifications/{notification.Id}/read", null);

        // Assert
        response.EnsureSuccessStatusCode();
        DbContext.ChangeTracker.Clear();
        var updatedNotification = await DbContext.Notifications.FindAsync(notification.Id);
        Assert.NotNull(updatedNotification);
        Assert.True(updatedNotification.IsRead);
    }

    [Fact]
    public async Task MarkAsRead_WithInvalidId_DoesNothing()
    {
        // Arrange
        var email = "markreadinvalid@example.com";
        await AuthenticateAsync(email);

        // Act
        var response = await Client.PostAsync($"/api/notifications/{Guid.NewGuid()}/read", null);

        // Assert
        response.EnsureSuccessStatusCode(); // API returns OK even if not found, to be idempotent/safe
    }

    [Fact]
    public async Task MarkAllAsRead_UpdatesAllUserNotifications()
    {
        // Arrange
        var email = "markallread@example.com";
        await AuthenticateAsync(email);
        var user = await DbContext.Users.FirstAsync(u => u.Email == email);

        DbContext.Notifications.AddRange(
            new Notification { UserId = user.Id, Title = "Unread 1", Body = "Body", Type = NotificationType.Info, IsRead = false },
            new Notification { UserId = user.Id, Title = "Unread 2", Body = "Body", Type = NotificationType.Info, IsRead = false }
        );
        await DbContext.SaveChangesAsync();

        // Act
        var response = await Client.PostAsync("/api/notifications/read-all", null);

        // Assert
        response.EnsureSuccessStatusCode();
        DbContext.ChangeTracker.Clear();
        var unreadCount = await DbContext.Notifications.CountAsync(n => n.UserId == user.Id && !n.IsRead);
        Assert.Equal(0, unreadCount);
    }

    [Fact]
    public async Task CreateTestNotification_PersistsToDatabase()
    {
        // Arrange
        var email = "testnotif@example.com";
        await AuthenticateAsync(email);
        var user = await DbContext.Users.FirstAsync(u => u.Email == email);

        // Act
        var response = await Client.PostAsync("/api/notifications/test", null);

        // Assert
        response.EnsureSuccessStatusCode();
        DbContext.ChangeTracker.Clear();
        var notification = await DbContext.Notifications.FirstOrDefaultAsync(n => n.UserId == user.Id && n.Title == "Welcome to Notifications!");
        Assert.NotNull(notification);
        Assert.Equal("This is a test notification to verify the system works.", notification.Body);
    }

    [Fact]
    public async Task Endpoints_RequireAuthentication_ReturnsUnauthorized()
    {
        // Act & Assert
        Assert.Equal(HttpStatusCode.Unauthorized, (await Client.GetAsync("/api/notifications")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Client.GetAsync("/api/notifications/unread-count")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Client.PostAsync($"/api/notifications/{Guid.NewGuid()}/read", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Client.PostAsync("/api/notifications/read-all", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Client.PostAsync("/api/notifications/test", null)).StatusCode);
    }
}
