using Microsoft.EntityFrameworkCore;
using Valora.Application.Common.Interfaces;
using Valora.Domain.Entities;
using Xunit;

namespace Valora.IntegrationTests;

[Collection("TestcontainersDatabase")]
public class WorkspaceRepositoryTests : BaseTestcontainersIntegrationTest
{
    public WorkspaceRepositoryTests(TestcontainersDatabaseFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task AddAsync_ShouldAddWorkspaceAndMembers()
    {
        // Arrange
        var repository = GetRequiredService<IWorkspaceRepository>();
        var ownerId = $"user_{Guid.NewGuid()}";

        var workspace = new Workspace
        {
            Id = Guid.NewGuid(),
            Name = "Test Workspace",
            Description = "A workspace for testing",
            OwnerId = ownerId,
            CreatedAt = DateTime.UtcNow
        };

        workspace.Members.Add(new WorkspaceMember
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspace.Id,
            UserId = ownerId,
            Role = WorkspaceRole.Owner,
            JoinedAt = DateTime.UtcNow
        });

        // Act
        await repository.AddAsync(workspace);
        await repository.SaveChangesAsync();

        // Assert
        // Clear change tracker to ensure we fetch from database, not from memory
        DbContext.ChangeTracker.Clear();

        var dbWorkspace = await DbContext.Workspaces
            .Include(w => w.Members)
            .FirstOrDefaultAsync(w => w.Id == workspace.Id);

        Assert.NotNull(dbWorkspace);
        Assert.Equal("Test Workspace", dbWorkspace.Name);
        Assert.Equal(ownerId, dbWorkspace.OwnerId);
        Assert.Single(dbWorkspace.Members);
        Assert.Equal(ownerId, dbWorkspace.Members.First().UserId);
        Assert.Equal(WorkspaceRole.Owner, dbWorkspace.Members.First().Role);
    }

    [Fact]
    public async Task GetUserWorkspacesAsync_ShouldReturnWorkspacesForUser()
    {
        // Arrange
        var repository = GetRequiredService<IWorkspaceRepository>();

        var targetUserId = $"user_{Guid.NewGuid()}";
        var otherUserId = $"other_{Guid.NewGuid()}";

        var workspace1 = new Workspace { Id = Guid.NewGuid(), Name = "WS 1", OwnerId = targetUserId };
        workspace1.Members.Add(new WorkspaceMember { Id = Guid.NewGuid(), WorkspaceId = workspace1.Id, UserId = targetUserId, Role = WorkspaceRole.Owner });

        var workspace2 = new Workspace { Id = Guid.NewGuid(), Name = "WS 2", OwnerId = otherUserId };
        workspace2.Members.Add(new WorkspaceMember { Id = Guid.NewGuid(), WorkspaceId = workspace2.Id, UserId = targetUserId, Role = WorkspaceRole.Editor });

        var workspace3 = new Workspace { Id = Guid.NewGuid(), Name = "WS 3", OwnerId = otherUserId };
        workspace3.Members.Add(new WorkspaceMember { Id = Guid.NewGuid(), WorkspaceId = workspace3.Id, UserId = otherUserId, Role = WorkspaceRole.Owner });

        await repository.AddAsync(workspace1);
        await repository.AddAsync(workspace2);
        await repository.AddAsync(workspace3);
        await repository.SaveChangesAsync();

        // Act
        var result = await repository.GetUserWorkspacesAsync(targetUserId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains(result, w => w.Id == workspace1.Id);
        Assert.Contains(result, w => w.Id == workspace2.Id);
        Assert.DoesNotContain(result, w => w.Id == workspace3.Id);
    }
}
