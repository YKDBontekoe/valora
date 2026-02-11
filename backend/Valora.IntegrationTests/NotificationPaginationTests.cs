using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Valora.Application.DTOs;
using Valora.Domain.Entities;
using Xunit;

namespace Valora.IntegrationTests;

public class NotificationPaginationTests : BaseIntegrationTest
{
    public NotificationPaginationTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task GetNotifications_WithOffset_SkipsRecords()
    {
        // Arrange
        var email = "pagination@example.com";
        await AuthenticateAsync(email);
        var user = await DbContext.Users.FirstAsync(u => u.Email == email);

        var notifications = Enumerable.Range(1, 10)
            .Select(i => new Notification
            {
                UserId = user.Id,
                Title = $"Notif {i}",
                Body = "Body",
                Type = NotificationType.Info,
                CreatedAt = DateTime.UtcNow.AddMinutes(-i) // Newer first (1 is newest, 10 is oldest)
            })
            .ToList();

        DbContext.Notifications.AddRange(notifications);
        await DbContext.SaveChangesAsync();

        // Act - Get first page (limit 5, offset 0)
        var responsePage1 = await Client.GetAsync("/api/notifications?limit=5&offset=0");
        responsePage1.EnsureSuccessStatusCode();
        var page1 = await responsePage1.Content.ReadFromJsonAsync<List<NotificationDto>>();

        // Act - Get second page (limit 5, offset 5)
        var responsePage2 = await Client.GetAsync("/api/notifications?limit=5&offset=5");
        responsePage2.EnsureSuccessStatusCode();
        var page2 = await responsePage2.Content.ReadFromJsonAsync<List<NotificationDto>>();

        // Assert
        Assert.NotNull(page1);
        Assert.Equal(5, page1.Count);
        Assert.Equal("Notif 1", page1[0].Title); // Newest first

        Assert.NotNull(page2);
        Assert.Equal(5, page2.Count);
        Assert.Equal("Notif 6", page2[0].Title); // 6th newest
    }

    [Fact]
    public async Task GetNotifications_WithOffsetBeyondTotal_ReturnsEmpty()
    {
        // Arrange
        var email = "pagination_empty@example.com";
        await AuthenticateAsync(email);
        var user = await DbContext.Users.FirstAsync(u => u.Email == email);

        DbContext.Notifications.Add(new Notification
        {
            UserId = user.Id,
            Title = "Notif 1",
            Body = "Body",
            Type = NotificationType.Info
        });
        await DbContext.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync("/api/notifications?limit=5&offset=10");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<List<NotificationDto>>();
        Assert.NotNull(result);
        Assert.Empty(result);
    }
}
