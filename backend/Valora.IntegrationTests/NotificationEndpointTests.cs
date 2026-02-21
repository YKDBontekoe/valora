using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Valora.Application.Common.Models;
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
        var result = await response.Content.ReadFromJsonAsync<CursorPagedResult<NotificationDto>>();
        Assert.NotNull(result);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetNotifications_ReturnsOnlyUserNotifications()
    {
        // Arrange
        var user1Email = "user1@example.com";
        var user2Email = "user2@example.com";

        await AuthenticateAsync(user1Email);
        var user1 = await DbContext.Users.FirstAsync(u => u.Email == user1Email);

        await AuthenticateAsync(user2Email);
        var user2 = await DbContext.Users.FirstAsync(u => u.Email == user2Email);

        DbContext.Notifications.AddRange(
            new Notification { UserId = user1.Id, Title = "User 1 Notif", Body = "Body", Type = NotificationType.Info },
            new Notification { UserId = user2.Id, Title = "User 2 Notif", Body = "Body", Type = NotificationType.Info }
        );
        await DbContext.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync("/api/notifications");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CursorPagedResult<NotificationDto>>();
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal("User 2 Notif", result.Items[0].Title);
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
                CreatedAt = DateTime.UtcNow.AddMinutes(i)
            })
            .ToList();

        DbContext.Notifications.AddRange(notifications);
        await DbContext.SaveChangesAsync();

        // Act
        var limit = 5;
        var response = await Client.GetAsync($"/api/notifications?limit={limit}");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CursorPagedResult<NotificationDto>>();
        Assert.NotNull(result);
        Assert.Equal(limit, result.Items.Count);
    }

    [Fact]
    public async Task GetNotifications_WithInvalidLimit_ReturnsBadRequest()
    {
        // Arrange
        await AuthenticateAsync();

        // Act & Assert
        Assert.Equal(HttpStatusCode.BadRequest, (await Client.GetAsync("/api/notifications?limit=0")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Client.GetAsync("/api/notifications?limit=101")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Client.GetAsync("/api/notifications?limit=-5")).StatusCode);
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
        var result = await response.Content.ReadFromJsonAsync<CursorPagedResult<NotificationDto>>();
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal("Unread", result.Items[0].Title);
        Assert.False(result.Items[0].IsRead);
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
        response.EnsureSuccessStatusCode();
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
    public async Task Endpoints_RequireAuthentication_ReturnsUnauthorized()
    {
        // Act & Assert
        Assert.Equal(HttpStatusCode.Unauthorized, (await Client.GetAsync("/api/notifications")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Client.GetAsync("/api/notifications/unread-count")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Client.PostAsync($"/api/notifications/{Guid.NewGuid()}/read", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Client.PostAsync("/api/notifications/read-all", null)).StatusCode);
    }
}
