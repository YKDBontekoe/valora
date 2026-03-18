using Microsoft.Extensions.DependencyInjection;
using Valora.Application.Common.Interfaces;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;
using Xunit;

namespace Valora.IntegrationTests;

[Collection("TestcontainersDatabase")]
public class WorkspaceRepositoryTests : BaseTestcontainersIntegrationTest
{
    public WorkspaceRepositoryTests(TestcontainersDatabaseFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task GetUserWorkspaceDtosAsync_ReturnsCorrectDtos()
    {
        // Arrange
        var context = GetRequiredService<ValoraDbContext>();
        var repository = GetRequiredService<IWorkspaceRepository>();

        var userId = $"test-user-{Guid.NewGuid()}";
        var workspace = new Workspace
        {
            Id = Guid.NewGuid(),
            Name = "Test Workspace",
            Description = "A workspace for testing",
            OwnerId = userId
        };

        var member = new WorkspaceMember
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspace.Id,
            UserId = userId,
            Role = WorkspaceRole.Owner
        };

        context.Workspaces.Add(workspace);
        context.WorkspaceMembers.Add(member);
        await context.SaveChangesAsync();

        // Act
        var dtos = await repository.GetUserWorkspaceDtosAsync(userId);

        // Assert
        Assert.Single(dtos);
        var dto = dtos.First();
        Assert.Equal(workspace.Id, dto.Id);
        Assert.Equal(workspace.Name, dto.Name);
        Assert.Equal(workspace.Description, dto.Description);
        Assert.Equal(1, dto.MemberCount);
        Assert.Equal(0, dto.SavedPropertyCount);
    }

    [Fact]
    public async Task AddSavedPropertyAsync_PersistsToDatabase()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
        var repository = scope.ServiceProvider.GetRequiredService<IWorkspaceRepository>();

        var userId = $"test-user-{Guid.NewGuid()}";

        var workspace = new Workspace
        {
            Id = Guid.NewGuid(),
            Name = "My Workspace",
            OwnerId = userId
        };

        var property = new Property
        {
            Id = Guid.NewGuid(),
            Address = "Kalverstraat 1",
            City = "Amsterdam"
        };

        context.Workspaces.Add(workspace);
        context.Properties.Add(property);
        await context.SaveChangesAsync();

        var savedProperty = new SavedProperty
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspace.Id,
            PropertyId = property.Id,
            AddedByUserId = userId,
            Notes = "Looks great"
        };

        // Act
        await repository.AddSavedPropertyAsync(savedProperty);
        await repository.SaveChangesAsync();

        // Assert
        using var assertScope = Factory.Services.CreateScope();
        var assertContext = assertScope.ServiceProvider.GetRequiredService<ValoraDbContext>();

        var dbSavedProperty = await assertContext.SavedProperties.FindAsync(savedProperty.Id);
        Assert.NotNull(dbSavedProperty);
        Assert.Equal(savedProperty.WorkspaceId, dbSavedProperty.WorkspaceId);
        Assert.Equal(savedProperty.PropertyId, dbSavedProperty.PropertyId);
        Assert.Equal("Looks great", dbSavedProperty.Notes);
    }

    [Fact]
    public async Task RemoveSavedPropertyAsync_DeletesFromDatabase()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
        var repository = scope.ServiceProvider.GetRequiredService<IWorkspaceRepository>();

        var userId = $"test-user-{Guid.NewGuid()}";

        var workspace = new Workspace
        {
            Id = Guid.NewGuid(),
            Name = "My Workspace",
            OwnerId = userId
        };

        var property = new Property
        {
            Id = Guid.NewGuid(),
            Address = "Damrak 1",
            City = "Amsterdam"
        };

        var savedProperty = new SavedProperty
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspace.Id,
            PropertyId = property.Id,
            AddedByUserId = userId
        };

        context.Workspaces.Add(workspace);
        context.Properties.Add(property);
        context.SavedProperties.Add(savedProperty);
        await context.SaveChangesAsync();

        // Act
        var spToRemove = await repository.GetSavedPropertyByIdAsync(savedProperty.Id);
        Assert.NotNull(spToRemove);

        await repository.RemoveSavedPropertyAsync(spToRemove);
        await repository.SaveChangesAsync();

        // Assert
        using var assertScope = Factory.Services.CreateScope();
        var assertContext = assertScope.ServiceProvider.GetRequiredService<ValoraDbContext>();

        var dbSavedProperty = await assertContext.SavedProperties.FindAsync(savedProperty.Id);
        Assert.Null(dbSavedProperty);
    }

    [Fact]
    public async Task GetPropertyAsync_ReturnsProperty()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
        var repository = scope.ServiceProvider.GetRequiredService<IWorkspaceRepository>();

        var propertyId = Guid.NewGuid();
        var property = new Property
        {
            Id = propertyId,
            Address = $"P.C. Hooftstraat {Guid.NewGuid()}",
            City = "Amsterdam",
            LivingAreaM2 = 120
        };

        context.Properties.Add(property);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetPropertyAsync(propertyId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(propertyId, result.Id);
        Assert.Equal(property.Address, result.Address);
        Assert.Equal("Amsterdam", result.City);
        Assert.Equal(120, result.LivingAreaM2);
    }
}