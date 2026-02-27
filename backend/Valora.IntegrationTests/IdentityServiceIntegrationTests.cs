using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Valora.Application.Common.Interfaces;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;
using Xunit;

namespace Valora.IntegrationTests;

[Collection("TestDatabase")]
public class IdentityServiceIntegrationTests : IAsyncLifetime
{
    private readonly TestDatabaseFixture _fixture;
    private IntegrationTestWebAppFactory _factory = null!;
    private IServiceScope _scope = null!;
    private IIdentityService _identityService = null!;
    private ValoraDbContext _dbContext = null!;

    public IdentityServiceIntegrationTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _factory = new IntegrationTestWebAppFactory("InMemory");
        _scope = _factory.Services.CreateScope();
        _dbContext = _scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
        _identityService = _scope.ServiceProvider.GetRequiredService<IIdentityService>();

        // Cleanup
        // Must delete dependent entities first
        _dbContext.Listings.RemoveRange(_dbContext.Listings);
        _dbContext.Notifications.RemoveRange(_dbContext.Notifications);
        _dbContext.RefreshTokens.RemoveRange(_dbContext.RefreshTokens);

        if (_dbContext.Users.Any())
        {
            _dbContext.Users.RemoveRange(_dbContext.Users);
        }
        if (_dbContext.Roles.Any())
        {
            _dbContext.Roles.RemoveRange(_dbContext.Roles);
        }
        await _dbContext.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        _scope.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task GetRolesForUsersAsync_ReturnsCorrectRoles_ForMultipleUsers()
    {
        // Arrange
        // Create roles
        await _identityService.EnsureRoleAsync("Admin");
        await _identityService.EnsureRoleAsync("User");

        // Create users
        var (result1, userId1) = await _identityService.CreateUserAsync("user1@test.com", "Password123!");
        Assert.True(result1.Succeeded);
        var (result2, userId2) = await _identityService.CreateUserAsync("user2@test.com", "Password123!");
        Assert.True(result2.Succeeded);
        var (result3, userId3) = await _identityService.CreateUserAsync("user3@test.com", "Password123!");
        Assert.True(result3.Succeeded);

        // Assign roles
        await _identityService.AddToRoleAsync(userId1, "Admin");
        await _identityService.AddToRoleAsync(userId2, "User");
        // user3 has no roles

        // Act
        // Fetch users to pass to the method (simulating what AdminService does)
        var users = _dbContext.Users.ToList(); // Fetch directly from context to be sure

        var rolesMap = await _identityService.GetRolesForUsersAsync(users);

        // Assert
        Assert.NotNull(rolesMap);

        // User 1 (Admin)
        Assert.True(rolesMap.ContainsKey(userId1));
        Assert.Contains("Admin", rolesMap[userId1]);
        Assert.Single(rolesMap[userId1]);

        // User 2 (User)
        Assert.True(rolesMap.ContainsKey(userId2));
        Assert.Contains("User", rolesMap[userId2]);
        Assert.Single(rolesMap[userId2]);

        // User 3 (No Roles)
        Assert.False(rolesMap.ContainsKey(userId3), "User without roles should not be in the result map");
    }

    [Fact]
    public async Task CreateUser_And_DeleteUser_Works()
    {
        // Arrange
        var email = "todelete@test.com";
        var (createResult, userId) = await _identityService.CreateUserAsync(email, "Password123!");
        Assert.True(createResult.Succeeded);

        // Act
        var deleteResult = await _identityService.DeleteUserAsync(userId);

        // Assert
        Assert.True(deleteResult.Succeeded);
        var user = await _identityService.GetUserByEmailAsync(email);
        Assert.Null(user);
    }

    [Fact]
    public async Task DeleteUserAsync_ShouldRemoveAllRelatedEntities()
    {
        // Arrange: Create User
        var email = "cascade.delete@test.com";
        var (createResult, userId) = await _identityService.CreateUserAsync(email, "Password123!");
        Assert.True(createResult.Succeeded);
        var user = await _dbContext.Users.FindAsync(userId);
        Assert.NotNull(user);

        // Arrange: Create Related Entities
        // 1. Owned Workspace
        var workspace = new Workspace
        {
            Name = "User's Workspace",
            OwnerId = userId
        };
        _dbContext.Workspaces.Add(workspace);
        await _dbContext.SaveChangesAsync();

        // 2. Membership in another workspace
        var otherUser = new ApplicationUser { UserName = "other@test.com", Email = "other@test.com" };
        _dbContext.Users.Add(otherUser);
        await _dbContext.SaveChangesAsync();

        var otherWorkspace = new Workspace
        {
            Name = "Other Workspace",
            OwnerId = otherUser.Id
        };
        _dbContext.Workspaces.Add(otherWorkspace);
        await _dbContext.SaveChangesAsync();

        var membership = new WorkspaceMember
        {
            WorkspaceId = otherWorkspace.Id,
            UserId = userId,
            Role = WorkspaceRole.Editor // WorkspaceRole.Member does not exist, Editor or Viewer does
        };
        _dbContext.WorkspaceMembers.Add(membership);

        // 3. Saved Listing (in owned workspace)
        var listing = new Listing
        {
            FundaId = "ext-1",
            Address = "Test St",
            City = "Test City",
            PostalCode = "1234AB",
            Price = 100000,
            Url = "http://example.com",
            ImageUrl = "http://example.com/img.jpg",
        };
        _dbContext.Listings.Add(listing);
        await _dbContext.SaveChangesAsync();

        var savedListing = new SavedListing
        {
            WorkspaceId = workspace.Id,
            ListingId = listing.Id,
            AddedByUserId = userId
        };
        _dbContext.SavedListings.Add(savedListing);

        // 4. Listing Comment
        var comment = new ListingComment
        {
            SavedListingId = savedListing.Id,
            UserId = userId,
            Content = "Test Comment"
        };
        _dbContext.ListingComments.Add(comment);

        // 5. Activity Log
        var log = new ActivityLog
        {
            ActorId = userId,
            WorkspaceId = workspace.Id,
            Type = ActivityLogType.ListingSaved,
            Summary = "User saved listing"
        };
        _dbContext.ActivityLogs.Add(log);

        // 6. User AI Profile
        var profile = new UserAiProfile
        {
            UserId = userId,
            Preferences = "{}"
        };
        _dbContext.UserAiProfiles.Add(profile);

        // 7. Refresh Token
        var token = RefreshToken.Create(userId, TimeProvider.System);
        _dbContext.RefreshTokens.Add(token);

        // 8. Notification
        var notification = new Notification
        {
            UserId = userId,
            Title = "Test",
            Body = "Test Body",
            Type = NotificationType.System
        };
        _dbContext.Notifications.Add(notification);

        await _dbContext.SaveChangesAsync();

        // Verify entities exist before deletion
        Assert.NotNull(await _dbContext.Workspaces.FindAsync(workspace.Id));
        Assert.NotNull(await _dbContext.WorkspaceMembers.FindAsync(membership.Id));
        Assert.NotNull(await _dbContext.SavedListings.FindAsync(savedListing.Id));
        Assert.NotNull(await _dbContext.ListingComments.FindAsync(comment.Id));
        Assert.NotNull(await _dbContext.ActivityLogs.FindAsync(log.Id));
        Assert.NotNull(await _dbContext.UserAiProfiles.FindAsync(profile.Id));
        Assert.NotNull(await _dbContext.RefreshTokens.FindAsync(token.Id));
        Assert.NotNull(await _dbContext.Notifications.FindAsync(notification.Id));

        // Act
        var deleteResult = await _identityService.DeleteUserAsync(userId);

        // Assert
        Assert.True(deleteResult.Succeeded);

        // Reload context to ensure we are not reading from cache
        // In this test setup, we are using the same context instance, but EF Core tracks entities.
        // Queries bypass cache for verification.

        var deletedUser = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(_dbContext.Users, u => u.Id == userId);
        Assert.Null(deletedUser);

        var deletedWorkspace = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(_dbContext.Workspaces, w => w.Id == workspace.Id);
        Assert.Null(deletedWorkspace);

        var deletedMembership = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(_dbContext.WorkspaceMembers, wm => wm.Id == membership.Id);
        Assert.Null(deletedMembership);

        var deletedSavedListing = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(_dbContext.SavedListings, sl => sl.Id == savedListing.Id);
        Assert.Null(deletedSavedListing);

        var deletedComment = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(_dbContext.ListingComments, c => c.Id == comment.Id);
        Assert.Null(deletedComment);

        var deletedLog = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(_dbContext.ActivityLogs, l => l.Id == log.Id);
        Assert.Null(deletedLog);

        var deletedProfile = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(_dbContext.UserAiProfiles, p => p.Id == profile.Id);
        Assert.Null(deletedProfile);

        var deletedToken = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(_dbContext.RefreshTokens, t => t.Id == token.Id);
        Assert.Null(deletedToken);

        var deletedNotification = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(_dbContext.Notifications, n => n.Id == notification.Id);
        Assert.Null(deletedNotification);
    }
}
