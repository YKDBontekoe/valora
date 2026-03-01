using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Valora.Application.DTOs;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;
using Xunit;

namespace Valora.IntegrationTests;

public class WorkspaceEndpointTests : BaseIntegrationTest
{
    public WorkspaceEndpointTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task CreateWorkspace_ReturnsCreated_WhenValid()
    {
        await AuthenticateAsync();
        var dto = new CreateWorkspaceDto("New Workspace", "Desc");

        var response = await Client.PostAsJsonAsync("/api/workspaces", dto);

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<WorkspaceDto>();
        Assert.NotNull(result);
        Assert.Equal(dto.Name, result.Name);
    }

    [Fact]
    public async Task GetUserWorkspaces_ReturnsList_WhenAuthenticated()
    {
        await AuthenticateAsync();
        // Create one first
        await Client.PostAsJsonAsync("/api/workspaces", new CreateWorkspaceDto("WS1", ""));

        var response = await Client.GetAsync("/api/workspaces");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<List<WorkspaceDto>>();
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task AddMember_ReturnsOk_WhenOwnerInvites()
    {
        await AuthenticateAsync();
        var createResponse = await Client.PostAsJsonAsync("/api/workspaces", new CreateWorkspaceDto("WS Member Test", ""));
        createResponse.EnsureSuccessStatusCode();
        var workspace = await createResponse.Content.ReadFromJsonAsync<WorkspaceDto>();
        Assert.NotNull(workspace);

        var inviteDto = new InviteMemberDto("newmember@test.com", WorkspaceRole.Editor);
        var response = await Client.PostAsJsonAsync($"/api/workspaces/{workspace.Id}/members", inviteDto);

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task SaveProperty_ReturnsOk_WhenValid()
    {
        // Use unique email to avoid conflicts with other tests
        var userId = "saveproperty@test.com";
        // AuthenticateAsync creates the user if not exists
        await AuthenticateAsync(userId, "Password123!");

        // Setup Property directly in DB
        Guid propertyId;
        // Using a fresh scope to ensure DB write is committed and visible
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
            var property = new Property { BagId = "999", Address = "Test Address" };
            db.Properties.Add(property);
            await db.SaveChangesAsync();
            propertyId = property.Id;
        }

        var createResponse = await Client.PostAsJsonAsync("/api/workspaces", new CreateWorkspaceDto("WS Property Test", ""));
        createResponse.EnsureSuccessStatusCode();
        var workspace = await createResponse.Content.ReadFromJsonAsync<WorkspaceDto>();
        Assert.NotNull(workspace);

        var saveRequest = new { PropertyId = propertyId, Notes = "Test Note" };
        var response = await Client.PostAsJsonAsync($"/api/workspaces/{workspace.Id}/properties", saveRequest);

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<SavedPropertyDto>();
        Assert.NotNull(result);
        Assert.Equal("Test Note", result.Notes);
    }
}
