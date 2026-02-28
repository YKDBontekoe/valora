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
        await AuthenticateAsync("user1@test.com");
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
        var ws = await wsResponse.Content.ReadFromJsonAsync<WorkspaceDto>();

        // Act
        var response = await Client.PostAsJsonAsync($"/api/workspaces/{ws!.Id}/properties", new SavePropertyDto(propertyId, ""));

        // Assert
        response.EnsureSuccessStatusCode();
        
        // Wait for background events
        await Task.Delay(500);

        var notifications = await Client.GetFromJsonAsync<PaginatedResponse<NotificationDto>>("/api/notifications");
        Assert.NotEmpty(notifications!.Items);
    }
}
