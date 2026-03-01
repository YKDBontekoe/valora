using Valora.Domain.Entities;
using Valora.Application.DTOs;
using Valora.Application.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Valora.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;

namespace Valora.IntegrationTests;

public class NotificationPipelineTests : BaseIntegrationTest
{
    public NotificationPipelineTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task SaveProperty_ShouldGenerateNotification()
    {
        // Arrange
        var user1Id = await AuthenticateAsync("user1@test.com");
        
        // Create a second user who will receive the notification
        var user2Email = "user2@test.com";
        var user2Password = "Password123!";
        var user2Response = await Client.PostAsJsonAsync("/api/auth/register", new { Email = user2Email, Password = user2Password, ConfirmPassword = user2Password });
        user2Response.EnsureSuccessStatusCode();
        
        Guid propertyId;
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
            var property = new Property { BagId = "N1", Address = "Notify St 1" };
            db.Properties.Add(property);
            await db.SaveChangesAsync();
            propertyId = property.Id;
        }

        var wsResponse = await Client.PostAsJsonAsync("/api/workspaces", new CreateWorkspaceDto("WS1", ""));
        wsResponse.EnsureSuccessStatusCode();
        var ws = await wsResponse.Content.ReadFromJsonAsync<WorkspaceDto>();
        Assert.NotNull(ws);

        // Add user2 to the workspace
        var inviteResponse = await Client.PostAsJsonAsync($"/api/workspaces/{ws.Id}/members", new InviteMemberDto(user2Email, WorkspaceRole.Editor));
        inviteResponse.EnsureSuccessStatusCode();

        // Act - User 1 saves a property
        var response = await Client.PostAsJsonAsync($"/api/workspaces/{ws.Id}/properties", new SavePropertyDto(propertyId, ""));

        // Assert
        response.EnsureSuccessStatusCode();
        
        // Switch to User 2 to check notifications
        await AuthenticateAsync(user2Email, user2Password);

        // Poll for notification
        List<NotificationDto>? notifications = null;
        for (int i = 0; i < 10; i++)
        {
            notifications = await Client.GetFromJsonAsync<List<NotificationDto>>("/api/notifications");
            if (notifications != null && notifications.Any(n => n.Title.Contains("Report Saved")))
                break;
            await Task.Delay(200);
        }

        Assert.NotNull(notifications);
        Assert.NotEmpty(notifications);
        Assert.Contains(notifications, n => n.Title.Contains("Report Saved"));
    }
}
