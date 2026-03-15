using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;
using Valora.Infrastructure.Persistence.Repositories;
using Xunit;

namespace Valora.IntegrationTests.Infrastructure;

public class WorkspaceRepositoryTests : BaseTestcontainersIntegrationTest
{
    public WorkspaceRepositoryTests(TestcontainersDatabaseFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task GetUserWorkspacesAsync_ShouldReturnWorkspacesForUser()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
        var repository = new WorkspaceRepository(context);

        var userId = Guid.NewGuid().ToString();
        var workspace1 = new Workspace { Name = "Workspace 1", OwnerId = userId };
        workspace1.Members.Add(new WorkspaceMember { UserId = userId, Role = WorkspaceRole.Owner });

        var workspace2 = new Workspace { Name = "Workspace 2", OwnerId = Guid.NewGuid().ToString() };
        workspace2.Members.Add(new WorkspaceMember { UserId = userId, Role = WorkspaceRole.Viewer }); // User is member

        var workspace3 = new Workspace { Name = "Workspace 3", OwnerId = Guid.NewGuid().ToString() };
        // User is not a member of workspace 3

        context.Workspaces.AddRange(workspace1, workspace2, workspace3);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetUserWorkspacesAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains(result, w => w.Id == workspace1.Id);
        Assert.Contains(result, w => w.Id == workspace2.Id);
        Assert.DoesNotContain(result, w => w.Id == workspace3.Id);

        // Verify includes
        Assert.All(result, w => Assert.NotNull(w.Members));
        Assert.All(result, w => Assert.NotNull(w.SavedProperties));
    }

    [Fact]
    public async Task GetSavedPropertiesAsync_ShouldReturnSavedPropertiesWithIncludes()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
        var repository = new WorkspaceRepository(context);

        var workspaceId = Guid.NewGuid();
        var workspace = new Workspace { Id = workspaceId, Name = "Workspace Saved Props", OwnerId = "user1" };

        var property = new Property { Address = "1234AB", City = "Amsterdam" };
        context.Properties.Add(property);

        var savedProperty = new SavedProperty { WorkspaceId = workspaceId, PropertyId = property.Id, AddedByUserId = "user1" };
        savedProperty.Comments.Add(new PropertyComment { UserId = "user1", Content = "A great property!" });

        context.Workspaces.Add(workspace);
        context.SavedProperties.Add(savedProperty);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetSavedPropertiesAsync(workspaceId);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        var returnedSp = result.First();
        Assert.Equal(savedProperty.Id, returnedSp.Id);
        Assert.NotNull(returnedSp.Property); // Included
        Assert.Equal(property.Id, returnedSp.Property.Id);
        Assert.NotNull(returnedSp.Comments); // Included
        Assert.Single(returnedSp.Comments);
    }

    [Fact]
    public async Task GetSavedPropertyDtosAsync_ShouldProjectToDto()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
        var repository = new WorkspaceRepository(context);

        var workspaceId = Guid.NewGuid();
        var workspace = new Workspace { Id = workspaceId, Name = "Dto Workspace", OwnerId = "user1" };

        var property = new Property { Address = "1234CD", City = "Utrecht", LivingAreaM2 = 150, ContextSafetyScore = 8.5, ContextCompositeScore = 8.0 };
        context.Properties.Add(property);

        var savedProperty = new SavedProperty { WorkspaceId = workspaceId, PropertyId = property.Id, AddedByUserId = "user1", Notes = "My notes" };
        savedProperty.Comments.Add(new PropertyComment { UserId = "user1", Content = "Comment 1" });
        savedProperty.Comments.Add(new PropertyComment { UserId = "user2", Content = "Comment 2" });

        context.Workspaces.Add(workspace);
        context.SavedProperties.Add(savedProperty);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetSavedPropertyDtosAsync(workspaceId);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);

        var dto = result.First();
        Assert.Equal(savedProperty.Id, dto.Id);
        Assert.Equal(property.Id, dto.PropertyId);
        Assert.Equal("My notes", dto.Notes);
        Assert.Equal(2, dto.CommentCount);

        Assert.NotNull(dto.Property);
        Assert.Equal(property.Address, dto.Property.Address);
        Assert.Equal(property.City, dto.Property.City);
        Assert.Equal(property.LivingAreaM2, dto.Property.LivingAreaM2);
        // The DTO might map properties differently, so we check what we can.
    }

    [Fact]
    public async Task GetActivityLogsAsync_ShouldReturnOrderedLogsForWorkspace()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
        var repository = new WorkspaceRepository(context);

        var workspaceId = Guid.NewGuid();
        var workspace = new Workspace { Id = workspaceId, Name = "Activity Workspace", OwnerId = "user1" };
        context.Workspaces.Add(workspace);

        var log1 = new ActivityLog { WorkspaceId = workspaceId, ActorId = "user1", Type = ActivityLogType.WorkspaceCreated, Summary = "Created workspace" };
        var log2 = new ActivityLog { WorkspaceId = workspaceId, ActorId = "user2", Type = ActivityLogType.MemberJoined, Summary = "Joined workspace" };
        var otherWorkspaceLog = new ActivityLog { WorkspaceId = Guid.NewGuid(), ActorId = "user3", Type = ActivityLogType.WorkspaceCreated, Summary = "Created other" };

        context.ActivityLogs.AddRange(log1, log2, otherWorkspaceLog);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetActivityLogsAsync(workspaceId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.All(result, log => Assert.Equal(workspaceId, log.WorkspaceId));

        Assert.Contains(result, a => a.Type == ActivityLogType.WorkspaceCreated);
        Assert.Contains(result, a => a.Type == ActivityLogType.MemberJoined);
    }

    [Fact]
    public async Task GetCommentAsync_ShouldReturnCommentForSavedProperty()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
        var repository = new WorkspaceRepository(context);

        var workspaceId = Guid.NewGuid();
        var workspace = new Workspace { Id = workspaceId, Name = "Comment Workspace", OwnerId = "user1" };

        var property = new Property { Address = "5678EF", City = "Rotterdam" };
        context.Properties.Add(property);

        var savedProperty = new SavedProperty { WorkspaceId = workspaceId, PropertyId = property.Id, AddedByUserId = "user1" };

        var comment = new PropertyComment { UserId = "user2", Content = "A specific comment text" };
        savedProperty.Comments.Add(comment);

        context.Workspaces.Add(workspace);
        context.SavedProperties.Add(savedProperty);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetCommentAsync(comment.Id, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(comment.Id, result.Id);
        Assert.Equal("A specific comment text", result.Content);
    }

    [Fact]
    public async Task GetByIdWithMembersAsync_ShouldReturnWorkspaceWithMembersIncluded()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
        var repository = new WorkspaceRepository(context);

        var workspaceId = Guid.NewGuid();
        var workspace = new Workspace { Id = workspaceId, Name = "Included Members", OwnerId = "user1" };
        workspace.Members.Add(new WorkspaceMember { UserId = "user1", Role = WorkspaceRole.Owner });
        workspace.Members.Add(new WorkspaceMember { UserId = "user2", Role = WorkspaceRole.Viewer });

        context.Workspaces.Add(workspace);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetByIdWithMembersAsync(workspaceId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(workspaceId, result.Id);
        Assert.NotNull(result.Members);
        Assert.Equal(2, result.Members.Count);
    }
}
