using System.Net.Http.Json;
using Valora.Application.DTOs;
using Valora.Domain.Entities;
using Xunit;

namespace Valora.IntegrationTests;

public class NotificationTests : TestcontainersIntegrationTest
{
    public NotificationTests(TestcontainersDatabaseFixture fixture) : base(fixture)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await AuthenticateAsync();
    }

    [Fact]
    public async Task GetNotifications_ReturnsEmptyList_WhenNoNotifications()
    {
        // Act
        var response = await Client.GetAsync("/api/notifications?unreadOnly=false&limit=50");

        // Assert
        response.EnsureSuccessStatusCode();
        var notifications = await response.Content.ReadFromJsonAsync<List<NotificationDto>>();
        Assert.NotNull(notifications);
        Assert.Empty(notifications);
    }

    [Fact]
    public async Task CreateNotification_And_GetNotifications_ReturnsNotification()
    {
        // Act
        // Use the test endpoint to create a notification
        var createResponse = await Client.PostAsync("/api/notifications/test", null);
        createResponse.EnsureSuccessStatusCode();

        var response = await Client.GetAsync("/api/notifications?unreadOnly=false&limit=50");

        // Assert
        response.EnsureSuccessStatusCode();
        var notifications = await response.Content.ReadFromJsonAsync<List<NotificationDto>>();
        Assert.NotNull(notifications);
        Assert.Single(notifications);
        Assert.Equal("Welcome to Notifications!", notifications[0].Title);
        Assert.False(notifications[0].IsRead);
    }

    [Fact]
    public async Task GetNotifications_UnreadOnly_ReturnsCorrectNotifications()
    {
        // Arrange
        // 1. Create a notification (unread by default)
        await CreateNotificationInDbAsync("Unread Notification");

        // 2. Create another one and mark it as read manually via DB context
        var user = DbContext.Users.First(u => u.Email == "test@example.com");
        DbContext.Notifications.Add(new Notification
        {
            UserId = user.Id,
            Title = "Read Notification",
            Body = "This should not appear in unread list",
            Type = NotificationType.Info,
            IsRead = true,
            CreatedAt = DateTime.UtcNow
        });
        await DbContext.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync("/api/notifications?unreadOnly=true&limit=50");

        // Assert
        response.EnsureSuccessStatusCode();
        var notifications = await response.Content.ReadFromJsonAsync<List<NotificationDto>>();
        Assert.NotNull(notifications);
        Assert.Single(notifications);
        Assert.Equal("Unread Notification", notifications[0].Title);
    }

    [Fact]
    public async Task GetUnreadCount_ReturnsCorrectCount()
    {
        // Arrange
        // Create 2 unread notifications
        await CreateNotificationInDbAsync("Notification 1");
        await CreateNotificationInDbAsync("Notification 2");

        // Act
        var response = await Client.GetAsync("/api/notifications/unread-count");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<UnreadCountResponse>();
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task MarkAsRead_UpdatesNotificationStatus()
    {
        // Arrange
        var notification = await CreateNotificationInDbAsync("Notification to Read");

        // Act
        var markResponse = await Client.PostAsync($"/api/notifications/{notification.Id}/read", null);

        // Assert
        markResponse.EnsureSuccessStatusCode();

        // Verify via DB for direct side-effect check
        DbContext.ChangeTracker.Clear();
        var updatedNotification = await DbContext.Notifications.FindAsync(notification.Id);
        Assert.True(updatedNotification!.IsRead);
    }

    [Fact]
    public async Task MarkAllAsRead_UpdatesAllNotifications()
    {
        // Arrange
        await CreateNotificationInDbAsync("Notification 1");
        await CreateNotificationInDbAsync("Notification 2");

        // Act
        var markAllResponse = await Client.PostAsync("/api/notifications/read-all", null);

        // Assert
        markAllResponse.EnsureSuccessStatusCode();

        var response = await Client.GetAsync("/api/notifications/unread-count");
        var result = await response.Content.ReadFromJsonAsync<UnreadCountResponse>();
        Assert.Equal(0, result!.Count);
    }

    private async Task<Notification> CreateNotificationInDbAsync(string title)
    {
        var user = DbContext.Users.First(u => u.Email == "test@example.com");
        var notification = new Notification
        {
            UserId = user.Id,
            Title = title,
            Body = "Body",
            Type = NotificationType.Info,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };
        DbContext.Notifications.Add(notification);
        await DbContext.SaveChangesAsync();
        return notification;
    }

    [Fact]
    public async Task GetNotifications_RespectsLimit()
    {
        // Arrange
        var user = DbContext.Users.First(u => u.Email == "test@example.com");

        for (int i = 0; i < 10; i++)
        {
            DbContext.Notifications.Add(new Notification
            {
                UserId = user.Id,
                Title = $"Notification {i}",
                Body = "Body",
                Type = NotificationType.Info,
                CreatedAt = DateTime.UtcNow.AddMinutes(-i) // Ensure order
            });
        }
        await DbContext.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync("/api/notifications?limit=5&unreadOnly=false");

        // Assert
        response.EnsureSuccessStatusCode();
        var notifications = await response.Content.ReadFromJsonAsync<List<NotificationDto>>();
        Assert.NotNull(notifications);
        Assert.Equal(5, notifications.Count);
    }

    private class UnreadCountResponse
    {
        public int Count { get; set; }
    }
}
