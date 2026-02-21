using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Valora.Application.Common.Models;
using Valora.Application.DTOs;
using Valora.Domain.Entities;
using Xunit;

namespace Valora.IntegrationTests;

public class NotificationCursorPaginationTests : BaseIntegrationTest
{
    public NotificationCursorPaginationTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task GetNotifications_WithCursor_ReturnsCorrectPages()
    {
        // Arrange
        var email = "cursor_pagination@example.com";
        await AuthenticateAsync(email, "Password123!");

        // Use the authenticated user
        var user = await DbContext.Users.FirstAsync(u => u.Email == email);
        var now = DateTime.UtcNow;

        // Create 10 notifications
        var notifications = Enumerable.Range(1, 10).Select(i => new Notification
        {
            UserId = user.Id,
            Title = $"Notif {i}",
            Body = "Body",
            Type = NotificationType.Info,
            CreatedAt = now.AddMinutes(-i), // 1 is newest (-1 min), 10 is oldest (-10 min)
            IsRead = false
        }).ToList();

        DbContext.Notifications.AddRange(notifications);
        await DbContext.SaveChangesAsync();

        // Act - Get Page 1 (Limit 5)
        var responsePage1 = await Client.GetAsync("/api/notifications?limit=5");
        responsePage1.EnsureSuccessStatusCode();
        var page1 = await responsePage1.Content.ReadFromJsonAsync<CursorPagedResult<NotificationDto>>();

        // Assert Page 1
        Assert.NotNull(page1);
        Assert.Equal(5, page1.Items.Count);
        Assert.True(page1.HasMore);
        Assert.NotNull(page1.NextCursor);

        Assert.Equal("Notif 1", page1.Items[0].Title);
        Assert.Equal("Notif 5", page1.Items[4].Title);

        // Act - Get Page 2 using NextCursor
        var responsePage2 = await Client.GetAsync($"/api/notifications?limit=5&cursor={page1.NextCursor}");
        responsePage2.EnsureSuccessStatusCode();
        var page2 = await responsePage2.Content.ReadFromJsonAsync<CursorPagedResult<NotificationDto>>();

        // Assert Page 2
        Assert.NotNull(page2);
        Assert.Equal(5, page2.Items.Count);

        Assert.Equal("Notif 6", page2.Items[0].Title);
        Assert.Equal("Notif 10", page2.Items[4].Title);

        // Act - Get Page 3 (Empty)
        Assert.NotNull(page2.NextCursor);
        var responsePage3 = await Client.GetAsync($"/api/notifications?limit=5&cursor={page2.NextCursor}");
        responsePage3.EnsureSuccessStatusCode();
        var page3 = await responsePage3.Content.ReadFromJsonAsync<CursorPagedResult<NotificationDto>>();

        Assert.NotNull(page3);
        Assert.Empty(page3.Items);
        Assert.False(page3.HasMore);
    }

    [Fact]
    public async Task GetNotifications_WithInvalidCursor_ReturnsFirstPageOrEmpty()
    {
        // Arrange
        var email = "invalid_cursor@example.com";
        await AuthenticateAsync(email);
        var user = await DbContext.Users.FirstAsync(u => u.Email == email);

        DbContext.Notifications.Add(new Notification
        {
            UserId = user.Id,
            Title = "Notif",
            Body = "Body",
            Type = NotificationType.Info
        });
        await DbContext.SaveChangesAsync();

        // Act - Invalid format
        var response = await Client.GetAsync("/api/notifications?cursor=invalid_format");

        // Assert - Should handle gracefully (e.g., ignore cursor and return first page, or return empty if logic dictates)
        // Current implementation: TryParse fails -> no filter applied -> returns first page
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CursorPagedResult<NotificationDto>>();
        Assert.NotNull(result);
        Assert.Single(result.Items);
    }
}
