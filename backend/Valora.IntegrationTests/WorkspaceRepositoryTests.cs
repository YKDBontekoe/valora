using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Valora.Application.Common.Interfaces;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;
using Xunit;

namespace Valora.IntegrationTests;

[Collection("TestDatabase")]
public class WorkspaceRepositoryTests
{
    private readonly TestDatabaseFixture _fixture;

    public WorkspaceRepositoryTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AddAsync_ShouldAddWorkspace()
    {
        // Arrange
        using var scope = _fixture.Factory!.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IWorkspaceRepository>();
        var context = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();

        var workspaceId = Guid.NewGuid();
        var ownerId = Guid.NewGuid().ToString();
        var user = new ApplicationUser { Id = ownerId, UserName = $"owner-{ownerId}", Email = $"owner-{ownerId}@test.com" };
        var workspace = new Workspace { Id = workspaceId, Name = "Test Add Workspace", OwnerId = ownerId };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Act
        await repository.AddAsync(workspace);
        await repository.SaveChangesAsync();

        // Assert
        var savedWorkspace = await context.Workspaces.FirstOrDefaultAsync(w => w.Id == workspaceId);
        Assert.NotNull(savedWorkspace);
        Assert.Equal("Test Add Workspace", savedWorkspace.Name);
        Assert.Equal(ownerId, savedWorkspace.OwnerId);
    }

    [Fact]
    public async Task GetUserWorkspacesAsync_ShouldReturnWorkspacesForUser()
    {
        // Arrange
        using var scope = _fixture.Factory!.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IWorkspaceRepository>();
        var context = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();

        var userId = Guid.NewGuid().ToString();
        var otherUserId = Guid.NewGuid().ToString();
        var user = new ApplicationUser { Id = userId, UserName = $"user-{userId}", Email = $"user-{userId}@test.com" };
        var otherUser = new ApplicationUser { Id = otherUserId, UserName = $"other-{otherUserId}", Email = $"other-{otherUserId}@test.com" };

        var workspace1 = new Workspace { Id = Guid.NewGuid(), Name = "Workspace 1", OwnerId = userId };
        var workspace2 = new Workspace { Id = Guid.NewGuid(), Name = "Workspace 2", OwnerId = userId };
        var workspace3 = new Workspace { Id = Guid.NewGuid(), Name = "Workspace 3", OwnerId = otherUserId };

        var member1 = new WorkspaceMember { WorkspaceId = workspace1.Id, UserId = userId, Role = WorkspaceRole.Owner };
        var member2 = new WorkspaceMember { WorkspaceId = workspace2.Id, UserId = userId, Role = WorkspaceRole.Viewer };
        var member3 = new WorkspaceMember { WorkspaceId = workspace3.Id, UserId = otherUserId, Role = WorkspaceRole.Owner };

        context.Users.AddRange(user, otherUser);
        context.Workspaces.AddRange(workspace1, workspace2, workspace3);
        context.WorkspaceMembers.AddRange(member1, member2, member3);
        await context.SaveChangesAsync();

        // Act
        var userWorkspaces = await repository.GetUserWorkspacesAsync(userId);

        // Assert
        Assert.Equal(2, userWorkspaces.Count);
        Assert.Contains(userWorkspaces, w => w.Id == workspace1.Id);
        Assert.Contains(userWorkspaces, w => w.Id == workspace2.Id);
        Assert.DoesNotContain(userWorkspaces, w => w.Id == workspace3.Id);
    }

    [Fact]
    public async Task GetWorkspaceDtoAndMemberStatusAsync_ShouldReturnDtoAndStatus()
    {
        // Arrange
        using var scope = _fixture.Factory!.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IWorkspaceRepository>();
        var context = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();

        var workspaceId = Guid.NewGuid();
        var userId = Guid.NewGuid().ToString();
        var ownerId = Guid.NewGuid().ToString();
        var user = new ApplicationUser { Id = userId, UserName = $"status-{userId}", Email = $"status-{userId}@test.com" };
        var owner = new ApplicationUser { Id = ownerId, UserName = $"owner-{ownerId}", Email = $"owner-{ownerId}@test.com" };
        var workspace = new Workspace { Id = workspaceId, Name = "Status Workspace", OwnerId = ownerId };
        var member = new WorkspaceMember { WorkspaceId = workspaceId, UserId = userId, Role = WorkspaceRole.Viewer };

        context.Users.AddRange(user, owner);
        context.Workspaces.Add(workspace);
        context.WorkspaceMembers.Add(member);
        await context.SaveChangesAsync();

        // Act
        var (dto, isMember) = await repository.GetWorkspaceDtoAndMemberStatusAsync(workspaceId, userId);

        // Assert
        Assert.NotNull(dto);
        Assert.Equal(workspaceId, dto.Id);
        Assert.Equal("Status Workspace", dto.Name);
        Assert.True(isMember);

        // Act - not a member
        var (_, isMemberFalse) = await repository.GetWorkspaceDtoAndMemberStatusAsync(workspaceId, "non-member-id");

        // Assert
        Assert.False(isMemberFalse);
    }

    [Fact]
    public async Task AddMemberAsync_ShouldAddMemberToWorkspace()
    {
         // Arrange
        using var scope = _fixture.Factory!.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IWorkspaceRepository>();
        var context = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();

        var workspaceId = Guid.NewGuid();
        var userId = Guid.NewGuid().ToString();
        var ownerId = Guid.NewGuid().ToString();
        var user = new ApplicationUser { Id = userId, UserName = $"new-member-{userId}", Email = $"new-{userId}@test.com" };
        var owner = new ApplicationUser { Id = ownerId, UserName = $"owner-{ownerId}", Email = $"owner-{ownerId}@test.com" };
        var workspace = new Workspace { Id = workspaceId, Name = "Member Add Workspace", OwnerId = ownerId };

        context.Users.AddRange(user, owner);
        context.Workspaces.Add(workspace);
        await context.SaveChangesAsync();

        var member = new WorkspaceMember { WorkspaceId = workspaceId, UserId = userId, Role = WorkspaceRole.Viewer };

        // Act
        await repository.AddMemberAsync(member);
        await repository.SaveChangesAsync();

        // Assert
        var savedMember = await context.WorkspaceMembers.FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId);
        Assert.NotNull(savedMember);
        Assert.Equal(WorkspaceRole.Viewer, savedMember.Role);
    }

    [Fact]
    public async Task GetSavedPropertiesAsync_ShouldReturnSavedProperties()
    {
         // Arrange
        using var scope = _fixture.Factory!.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IWorkspaceRepository>();
        var context = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();

        var workspaceId = Guid.NewGuid();
        var ownerId = Guid.NewGuid().ToString();
        var owner = new ApplicationUser { Id = ownerId, UserName = $"owner-{ownerId}", Email = $"owner-{ownerId}@test.com" };
        var addedByUserId = Guid.NewGuid().ToString();
        var addedByUser = new ApplicationUser { Id = addedByUserId, UserName = $"user-{addedByUserId}", Email = $"user-{addedByUserId}@test.com" };

        var workspace = new Workspace { Id = workspaceId, Name = "Saved Props Workspace", OwnerId = ownerId };

        var propertyId1 = Guid.NewGuid();
        var propertyId2 = Guid.NewGuid();
        var propertyId3 = Guid.NewGuid();

        var property1 = new Property { Id = propertyId1, BagId = "BAG_1", Address = "Address 1", City = "City", Latitude = 1, Longitude = 1 };
        var property2 = new Property { Id = propertyId2, BagId = "BAG_2", Address = "Address 2", City = "City", Latitude = 2, Longitude = 2 };
        var property3 = new Property { Id = propertyId3, BagId = "BAG_3", Address = "Address 3", City = "City", Latitude = 3, Longitude = 3 };

        var otherWorkspaceId = Guid.NewGuid();
        var otherWorkspace = new Workspace { Id = otherWorkspaceId, Name = "Other Workspace", OwnerId = ownerId };

        var savedProp1 = new SavedProperty { Id = Guid.NewGuid(), WorkspaceId = workspaceId, PropertyId = propertyId1, AddedByUserId = addedByUserId, CreatedAt = DateTime.UtcNow };
        var savedProp2 = new SavedProperty { Id = Guid.NewGuid(), WorkspaceId = workspaceId, PropertyId = propertyId2, AddedByUserId = addedByUserId, CreatedAt = DateTime.UtcNow.AddMinutes(-10) };
        var savedProp3 = new SavedProperty { Id = Guid.NewGuid(), WorkspaceId = otherWorkspaceId, PropertyId = propertyId3, AddedByUserId = addedByUserId }; // Different workspace

        context.Users.AddRange(owner, addedByUser);
        context.Workspaces.Add(workspace);
        context.Workspaces.Add(otherWorkspace);
        context.Properties.AddRange(property1, property2, property3);
        context.SavedProperties.AddRange(savedProp1, savedProp2, savedProp3);
        await context.SaveChangesAsync();

        // Act
        var savedProperties = await repository.GetSavedPropertiesAsync(workspaceId);

        // Assert
        Assert.Equal(2, savedProperties.Count);
        Assert.Contains(savedProperties, sp => sp.PropertyId == propertyId1);
        Assert.Contains(savedProperties, sp => sp.PropertyId == propertyId2);
        Assert.DoesNotContain(savedProperties, sp => sp.PropertyId == propertyId3);

        // Verify ordering (descending by CreatedAt)
        Assert.Equal(propertyId1, savedProperties[0].PropertyId);
        Assert.Equal(propertyId2, savedProperties[1].PropertyId);
    }
}
