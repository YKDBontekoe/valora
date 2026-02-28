using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Valora.Application.DTOs;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;
using Xunit;

namespace Valora.IntegrationTests;

public class WorkspaceIntegrationTests : BaseTestcontainersIntegrationTest
{
    public WorkspaceIntegrationTests(TestcontainersDatabaseFixture fixture) : base(fixture)
    {
    }

    // --- Member Management ---

    [Fact]
    public async Task AddMember_ShouldSucceed_WhenOwnerInvites()
    {
        // Arrange
        var ownerEmail = "owner@test.com";
        await AuthenticateAsync(ownerEmail);
        var createResponse = await Client.PostAsJsonAsync("/api/workspaces", new CreateWorkspaceDto("WS Member Test", ""));
        var workspace = await createResponse.Content.ReadFromJsonAsync<WorkspaceDto>();

        var inviteDto = new InviteMemberDto("newmember@test.com", WorkspaceRole.Editor);

        // Act
        var response = await Client.PostAsJsonAsync($"/api/workspaces/{workspace!.Id}/members", inviteDto);

        // Assert
        response.EnsureSuccessStatusCode();

        // Verify side effect: Member exists in DB
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
        var member = await db.WorkspaceMembers.FirstOrDefaultAsync(m => m.WorkspaceId == workspace.Id && m.InvitedEmail == inviteDto.Email);
        Assert.NotNull(member);
        Assert.Equal(WorkspaceRole.Editor, member.Role);
    }

    [Fact]
    public async Task AddMember_ShouldFail_WhenNonOwnerInvites()
    {
        // Arrange
        var ownerEmail = "owner2@test.com";
        await AuthenticateAsync(ownerEmail);
        var createResponse = await Client.PostAsJsonAsync("/api/workspaces", new CreateWorkspaceDto("WS NonOwner Test", ""));
        var workspace = await createResponse.Content.ReadFromJsonAsync<WorkspaceDto>();

        // Switch to Editor user
        var editorEmail = "editor@test.com";
        await AuthenticateAsync(editorEmail); // Creates user and gets token

        // Add editor to workspace manually
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
            var user = await db.Users.FirstAsync(u => u.Email == editorEmail);
            db.WorkspaceMembers.Add(new WorkspaceMember
            {
                WorkspaceId = workspace!.Id,
                UserId = user.Id,
                Role = WorkspaceRole.Editor,
                JoinedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var inviteDto = new InviteMemberDto("random@test.com", WorkspaceRole.Viewer);

        // Act
        var response = await Client.PostAsJsonAsync($"/api/workspaces/{workspace!.Id}/members", inviteDto);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RemoveMember_ShouldSucceed_WhenOwnerRemoves()
    {
        // Arrange
        var ownerEmail = "owner3@test.com";
        await AuthenticateAsync(ownerEmail);
        var createResponse = await Client.PostAsJsonAsync("/api/workspaces", new CreateWorkspaceDto("WS Remove Test", ""));
        var workspace = await createResponse.Content.ReadFromJsonAsync<WorkspaceDto>();

        // Add a member directly
        Guid memberId;
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
            var member = new WorkspaceMember
            {
                WorkspaceId = workspace!.Id,
                InvitedEmail = "toremove@test.com",
                Role = WorkspaceRole.Viewer
            };
            db.WorkspaceMembers.Add(member);
            await db.SaveChangesAsync();
            memberId = member.Id;
        }

        // Act
        var response = await Client.DeleteAsync($"/api/workspaces/{workspace!.Id}/members/{memberId}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify side effect: Member removed
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
            var exists = await db.WorkspaceMembers.AnyAsync(m => m.Id == memberId);
            Assert.False(exists);
        }
    }

    [Fact]
    public async Task RemoveMember_ShouldFail_WhenSelfRemoval()
    {
        // Arrange
        var ownerEmail = "owner4@test.com";
        await AuthenticateAsync(ownerEmail);
        var createResponse = await Client.PostAsJsonAsync("/api/workspaces", new CreateWorkspaceDto("WS Self Remove Test", ""));
        var workspace = await createResponse.Content.ReadFromJsonAsync<WorkspaceDto>();

        // Get owner member ID
        Guid ownerMemberId;
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
            var member = await db.WorkspaceMembers.FirstAsync(m => m.WorkspaceId == workspace!.Id && m.Role == WorkspaceRole.Owner);
            ownerMemberId = member.Id;
        }

        // Act
        // Backend maps InvalidOperationException to 409 Conflict per memory, or 500 depending on middleware.
        // Memory says: "Backend throws InvalidOperationException (mapped to 409)"
        var response = await Client.DeleteAsync($"/api/workspaces/{workspace!.Id}/members/{ownerMemberId}");

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    // --- Saved Properties ---

    [Fact]
    public async Task SaveProperty_ShouldSucceed_WhenValid()
    {
        // Arrange
        var ownerEmail = "owner5@test.com";
        await AuthenticateAsync(ownerEmail);

        // Create Property
        Guid propertyId;
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
            var property = new Property { BagId = "101", Address = "Valid Property St 1" };
            db.Properties.Add(property);
            await db.SaveChangesAsync();
            propertyId = property.Id;
        }

        var createResponse = await Client.PostAsJsonAsync("/api/workspaces", new CreateWorkspaceDto("WS Save Property Test", ""));
        var workspace = await createResponse.Content.ReadFromJsonAsync<WorkspaceDto>();

        var saveDto = new SavePropertyDto(propertyId, "My notes");

        // Act
        var response = await Client.PostAsJsonAsync($"/api/workspaces/{workspace!.Id}/properties", saveDto);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<SavedPropertyDto>();
        Assert.NotNull(result);
        Assert.Equal("My notes", result.Notes);

        // Verify DB
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
            var saved = await db.SavedProperties.FirstOrDefaultAsync(sl => sl.WorkspaceId == workspace.Id && sl.PropertyId == propertyId);
            Assert.NotNull(saved);
        }
    }

    [Fact]
    public async Task SaveProperty_ShouldBeIdempotent()
    {
        // Arrange
        var ownerEmail = "owner6@test.com";
        await AuthenticateAsync(ownerEmail);

        Guid propertyId;
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
            var property = new Property { BagId = "102", Address = "Idempotent St 1" };
            db.Properties.Add(property);
            await db.SaveChangesAsync();
            propertyId = property.Id;
        }

        var createResponse = await Client.PostAsJsonAsync("/api/workspaces", new CreateWorkspaceDto("WS Idempotent Test", ""));
        var workspace = await createResponse.Content.ReadFromJsonAsync<WorkspaceDto>();

        var saveDto = new SavePropertyDto(propertyId, "Note 1");

        // Act 1
        await Client.PostAsJsonAsync($"/api/workspaces/{workspace!.Id}/properties", saveDto);

        // Act 2 (Repeat)
        var response = await Client.PostAsJsonAsync($"/api/workspaces/{workspace!.Id}/properties", saveDto);

        // Assert
        response.EnsureSuccessStatusCode();

        // Verify only 1 record
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
            var count = await db.SavedProperties.CountAsync(sl => sl.WorkspaceId == workspace.Id && sl.PropertyId == propertyId);
            Assert.Equal(1, count);
        }
    }

    [Fact]
    public async Task RemoveSavedProperty_ShouldSucceed_WhenOwner()
    {
        // Arrange
        var ownerEmail = "owner7@test.com";
        await AuthenticateAsync(ownerEmail);

        Guid propertyId;
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
            var property = new Property { BagId = "103", Address = "Remove St 1" };
            db.Properties.Add(property);
            await db.SaveChangesAsync();
            propertyId = property.Id;
        }

        var createResponse = await Client.PostAsJsonAsync("/api/workspaces", new CreateWorkspaceDto("WS Remove Property Test", ""));
        var workspace = await createResponse.Content.ReadFromJsonAsync<WorkspaceDto>();

        // Save first
        var saveResponse = await Client.PostAsJsonAsync($"/api/workspaces/{workspace!.Id}/properties", new SavePropertyDto(propertyId, ""));
        var savedProperty = await saveResponse.Content.ReadFromJsonAsync<SavedPropertyDto>();

        // Act
        var response = await Client.DeleteAsync($"/api/workspaces/{workspace!.Id}/properties/{savedProperty!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify DB
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
            var exists = await db.SavedProperties.AnyAsync(sl => sl.Id == savedProperty.Id);
            Assert.False(exists);
        }
    }

    [Fact]
    public async Task RemoveSavedProperty_ShouldFail_WhenViewer()
    {
        // Arrange
        var ownerEmail = "owner8@test.com";
        await AuthenticateAsync(ownerEmail);

        Guid propertyId;
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
            var property = new Property { BagId = "104", Address = "Viewer Fail St 1" };
            db.Properties.Add(property);
            await db.SaveChangesAsync();
            propertyId = property.Id;
        }

        var createResponse = await Client.PostAsJsonAsync("/api/workspaces", new CreateWorkspaceDto("WS Viewer Property Test", ""));
        var workspace = await createResponse.Content.ReadFromJsonAsync<WorkspaceDto>();

        // Save property as owner
        var saveResponse = await Client.PostAsJsonAsync($"/api/workspaces/{workspace!.Id}/properties", new SavePropertyDto(propertyId, ""));
        var savedProperty = await saveResponse.Content.ReadFromJsonAsync<SavedPropertyDto>();

        // Create Viewer user
        var viewerEmail = "viewer@test.com";
        await AuthenticateAsync(viewerEmail);

        // Add viewer to workspace
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
            var user = await db.Users.FirstAsync(u => u.Email == viewerEmail);
            db.WorkspaceMembers.Add(new WorkspaceMember
            {
                WorkspaceId = workspace!.Id,
                UserId = user.Id,
                Role = WorkspaceRole.Viewer,
                JoinedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        // Act
        var response = await Client.DeleteAsync($"/api/workspaces/{workspace!.Id}/properties/{savedProperty!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // --- Comments ---

    [Fact]
    public async Task AddComment_ShouldSucceed()
    {
        // Arrange
        var ownerEmail = "owner9@test.com";
        await AuthenticateAsync(ownerEmail);

        Guid propertyId;
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
            var property = new Property { BagId = "105", Address = "Comment St 1" };
            db.Properties.Add(property);
            await db.SaveChangesAsync();
            propertyId = property.Id;
        }

        var createResponse = await Client.PostAsJsonAsync("/api/workspaces", new CreateWorkspaceDto("WS Comment Test", ""));
        var workspace = await createResponse.Content.ReadFromJsonAsync<WorkspaceDto>();

        var saveResponse = await Client.PostAsJsonAsync($"/api/workspaces/{workspace!.Id}/properties", new SavePropertyDto(propertyId, ""));
        var savedProperty = await saveResponse.Content.ReadFromJsonAsync<SavedPropertyDto>();

        var commentDto = new AddCommentDto("This is a comment", null);

        // Act
        var response = await Client.PostAsJsonAsync($"/api/workspaces/{workspace!.Id}/properties/{savedProperty!.Id}/comments", commentDto);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CommentDto>();
        Assert.Equal("This is a comment", result!.Content);

        // Verify DB
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
            var comment = await db.PropertyComments.FirstOrDefaultAsync(c => c.SavedPropertyId == savedProperty.Id);
            Assert.NotNull(comment);
            Assert.Equal("This is a comment", comment.Content);
        }
    }

    [Fact]
    public async Task GetComments_ShouldReturnThreadedComments()
    {
        // Arrange
        var ownerEmail = "owner10@test.com";
        await AuthenticateAsync(ownerEmail);

        Guid propertyId;
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
            var property = new Property { BagId = "106", Address = "Threaded St 1" };
            db.Properties.Add(property);
            await db.SaveChangesAsync();
            propertyId = property.Id;
        }

        var createResponse = await Client.PostAsJsonAsync("/api/workspaces", new CreateWorkspaceDto("WS Thread Test", ""));
        var workspace = await createResponse.Content.ReadFromJsonAsync<WorkspaceDto>();

        var saveResponse = await Client.PostAsJsonAsync($"/api/workspaces/{workspace!.Id}/properties", new SavePropertyDto(propertyId, ""));
        var savedProperty = await saveResponse.Content.ReadFromJsonAsync<SavedPropertyDto>();

        // Add parent comment via DB directly
        Guid parentId;
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
            var user = await db.Users.FirstAsync(u => u.Email == ownerEmail);
            var parent = new PropertyComment
            {
                SavedPropertyId = savedProperty!.Id,
                UserId = user.Id,
                Content = "Parent",
                CreatedAt = DateTime.UtcNow.AddMinutes(-10)
            };
            db.PropertyComments.Add(parent);
            await db.SaveChangesAsync();
            parentId = parent.Id;

            // Add child comment
            var child = new PropertyComment
            {
                SavedPropertyId = savedProperty.Id,
                UserId = user.Id,
                Content = "Child",
                ParentCommentId = parentId,
                CreatedAt = DateTime.UtcNow
            };
            db.PropertyComments.Add(child);
            await db.SaveChangesAsync();
        }

        // Act
        var response = await Client.GetAsync($"/api/workspaces/{workspace!.Id}/properties/{savedProperty!.Id}/comments");

        // Assert
        response.EnsureSuccessStatusCode();
        var comments = await response.Content.ReadFromJsonAsync<List<CommentDto>>();

        Assert.NotNull(comments);
        Assert.Single(comments); // Only 1 root comment
        Assert.Equal("Parent", comments[0].Content);
        Assert.Single(comments[0].Replies);
        Assert.Equal("Child", comments[0].Replies[0].Content);
    }

    // --- RBAC ---

    [Fact]
    public async Task GetWorkspace_ShouldFail_WhenNotMember()
    {
        // Arrange
        var ownerEmail = "owner11@test.com";
        await AuthenticateAsync(ownerEmail);
        var createResponse = await Client.PostAsJsonAsync("/api/workspaces", new CreateWorkspaceDto("WS Access Test", ""));
        var workspace = await createResponse.Content.ReadFromJsonAsync<WorkspaceDto>();

        // Switch to intruder
        await AuthenticateAsync("intruder@test.com");

        // Act
        var response = await Client.GetAsync($"/api/workspaces/{workspace!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
