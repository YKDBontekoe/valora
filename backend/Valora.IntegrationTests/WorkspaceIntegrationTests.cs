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

    // --- Saved Listings ---

    [Fact]
    public async Task SaveListing_ShouldSucceed_WhenValid()
    {
        // Arrange
        var ownerEmail = "owner5@test.com";
        await AuthenticateAsync(ownerEmail);

        // Create Listing
        Guid listingId;
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
            var listing = new Listing { FundaId = "101", Address = "Valid Listing St 1" };
            db.Listings.Add(listing);
            await db.SaveChangesAsync();
            listingId = listing.Id;
        }

        var createResponse = await Client.PostAsJsonAsync("/api/workspaces", new CreateWorkspaceDto("WS Save Listing Test", ""));
        var workspace = await createResponse.Content.ReadFromJsonAsync<WorkspaceDto>();

        var saveDto = new SaveListingDto(listingId, "My notes");

        // Act
        var response = await Client.PostAsJsonAsync($"/api/workspaces/{workspace!.Id}/listings", saveDto);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<SavedListingDto>();
        Assert.NotNull(result);
        Assert.Equal("My notes", result.Notes);

        // Verify DB
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
            var saved = await db.SavedListings.FirstOrDefaultAsync(sl => sl.WorkspaceId == workspace.Id && sl.ListingId == listingId);
            Assert.NotNull(saved);
        }
    }

    [Fact]
    public async Task SaveListing_ShouldBeIdempotent()
    {
        // Arrange
        var ownerEmail = "owner6@test.com";
        await AuthenticateAsync(ownerEmail);

        Guid listingId;
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
            var listing = new Listing { FundaId = "102", Address = "Idempotent St 1" };
            db.Listings.Add(listing);
            await db.SaveChangesAsync();
            listingId = listing.Id;
        }

        var createResponse = await Client.PostAsJsonAsync("/api/workspaces", new CreateWorkspaceDto("WS Idempotent Test", ""));
        var workspace = await createResponse.Content.ReadFromJsonAsync<WorkspaceDto>();

        var saveDto = new SaveListingDto(listingId, "Note 1");

        // Act 1
        await Client.PostAsJsonAsync($"/api/workspaces/{workspace!.Id}/listings", saveDto);

        // Act 2 (Repeat)
        var response = await Client.PostAsJsonAsync($"/api/workspaces/{workspace!.Id}/listings", saveDto);

        // Assert
        response.EnsureSuccessStatusCode();

        // Verify only 1 record
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
            var count = await db.SavedListings.CountAsync(sl => sl.WorkspaceId == workspace.Id && sl.ListingId == listingId);
            Assert.Equal(1, count);
        }
    }

    [Fact]
    public async Task RemoveSavedListing_ShouldSucceed_WhenOwner()
    {
        // Arrange
        var ownerEmail = "owner7@test.com";
        await AuthenticateAsync(ownerEmail);

        Guid listingId;
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
            var listing = new Listing { FundaId = "103", Address = "Remove St 1" };
            db.Listings.Add(listing);
            await db.SaveChangesAsync();
            listingId = listing.Id;
        }

        var createResponse = await Client.PostAsJsonAsync("/api/workspaces", new CreateWorkspaceDto("WS Remove Listing Test", ""));
        var workspace = await createResponse.Content.ReadFromJsonAsync<WorkspaceDto>();

        // Save first
        var saveResponse = await Client.PostAsJsonAsync($"/api/workspaces/{workspace!.Id}/listings", new SaveListingDto(listingId, ""));
        var savedListing = await saveResponse.Content.ReadFromJsonAsync<SavedListingDto>();

        // Act
        var response = await Client.DeleteAsync($"/api/workspaces/{workspace!.Id}/listings/{savedListing!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify DB
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
            var exists = await db.SavedListings.AnyAsync(sl => sl.Id == savedListing.Id);
            Assert.False(exists);
        }
    }

    [Fact]
    public async Task RemoveSavedListing_ShouldFail_WhenViewer()
    {
        // Arrange
        var ownerEmail = "owner8@test.com";
        await AuthenticateAsync(ownerEmail);

        Guid listingId;
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
            var listing = new Listing { FundaId = "104", Address = "Viewer Fail St 1" };
            db.Listings.Add(listing);
            await db.SaveChangesAsync();
            listingId = listing.Id;
        }

        var createResponse = await Client.PostAsJsonAsync("/api/workspaces", new CreateWorkspaceDto("WS Viewer Listing Test", ""));
        var workspace = await createResponse.Content.ReadFromJsonAsync<WorkspaceDto>();

        // Save listing as owner
        var saveResponse = await Client.PostAsJsonAsync($"/api/workspaces/{workspace!.Id}/listings", new SaveListingDto(listingId, ""));
        var savedListing = await saveResponse.Content.ReadFromJsonAsync<SavedListingDto>();

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
        var response = await Client.DeleteAsync($"/api/workspaces/{workspace!.Id}/listings/{savedListing!.Id}");

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

        Guid listingId;
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
            var listing = new Listing { FundaId = "105", Address = "Comment St 1" };
            db.Listings.Add(listing);
            await db.SaveChangesAsync();
            listingId = listing.Id;
        }

        var createResponse = await Client.PostAsJsonAsync("/api/workspaces", new CreateWorkspaceDto("WS Comment Test", ""));
        var workspace = await createResponse.Content.ReadFromJsonAsync<WorkspaceDto>();

        var saveResponse = await Client.PostAsJsonAsync($"/api/workspaces/{workspace!.Id}/listings", new SaveListingDto(listingId, ""));
        var savedListing = await saveResponse.Content.ReadFromJsonAsync<SavedListingDto>();

        var commentDto = new AddCommentDto("This is a comment", null);

        // Act
        var response = await Client.PostAsJsonAsync($"/api/workspaces/{workspace!.Id}/listings/{savedListing!.Id}/comments", commentDto);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CommentDto>();
        Assert.Equal("This is a comment", result!.Content);

        // Verify DB
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
            var comment = await db.ListingComments.FirstOrDefaultAsync(c => c.SavedListingId == savedListing.Id);
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

        Guid listingId;
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
            var listing = new Listing { FundaId = "106", Address = "Threaded St 1" };
            db.Listings.Add(listing);
            await db.SaveChangesAsync();
            listingId = listing.Id;
        }

        var createResponse = await Client.PostAsJsonAsync("/api/workspaces", new CreateWorkspaceDto("WS Thread Test", ""));
        var workspace = await createResponse.Content.ReadFromJsonAsync<WorkspaceDto>();

        var saveResponse = await Client.PostAsJsonAsync($"/api/workspaces/{workspace!.Id}/listings", new SaveListingDto(listingId, ""));
        var savedListing = await saveResponse.Content.ReadFromJsonAsync<SavedListingDto>();

        // Add parent comment via DB directly
        Guid parentId;
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
            var user = await db.Users.FirstAsync(u => u.Email == ownerEmail);
            var parent = new ListingComment
            {
                SavedListingId = savedListing!.Id,
                UserId = user.Id,
                Content = "Parent",
                CreatedAt = DateTime.UtcNow.AddMinutes(-10)
            };
            db.ListingComments.Add(parent);
            await db.SaveChangesAsync();
            parentId = parent.Id;

            // Add child comment
            var child = new ListingComment
            {
                SavedListingId = savedListing.Id,
                UserId = user.Id,
                Content = "Child",
                ParentCommentId = parentId,
                CreatedAt = DateTime.UtcNow
            };
            db.ListingComments.Add(child);
            await db.SaveChangesAsync();
        }

        // Act
        var response = await Client.GetAsync($"/api/workspaces/{workspace!.Id}/listings/{savedListing!.Id}/comments");

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
