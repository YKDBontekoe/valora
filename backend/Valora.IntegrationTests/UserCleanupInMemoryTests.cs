using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Valora.Application.Common.Interfaces;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;
using Xunit;

namespace Valora.IntegrationTests;

// This class deliberately avoids the shared Testcontainers fixture to force InMemory execution
// and verify the fallback logic in IdentityService.DeleteUserAsync.
public class UserCleanupInMemoryTests : IAsyncLifetime
{
    private IntegrationTestWebAppFactory _factory = null!;
    private IServiceScope _scope = null!;
    private ValoraDbContext _dbContext = null!;
    private IIdentityService _identityService = null!;

    public async Task InitializeAsync()
    {
        // Force InMemory database by using a specific connection string format
        _factory = new IntegrationTestWebAppFactory("InMemory:ForceCleanupTest");
        _scope = _factory.Services.CreateScope();
        _dbContext = _scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
        _identityService = _scope.ServiceProvider.GetRequiredService<IIdentityService>();

        // Ensure database is created
        await _dbContext.Database.EnsureCreatedAsync();

        // Ensure database is clean
        _dbContext.Notifications.RemoveRange(_dbContext.Notifications);
        _dbContext.RefreshTokens.RemoveRange(_dbContext.RefreshTokens);
        if (_dbContext.Users.Any())
        {
            _dbContext.Users.RemoveRange(_dbContext.Users);
        }
        await _dbContext.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        _scope.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task DeleteUser_ShouldRemoveDependentNotifications_WhenUserIsDeleted_InMemory()
    {
        // Arrange
        var user = new ApplicationUser
        {
            UserName = "inmemory_user",
            Email = "inmemory@example.com",
            EmailConfirmed = true
        };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var notification = new Notification
        {
            UserId = user.Id,
            Title = "InMemory Test",
            Body = "This should be deleted",
            Type = NotificationType.Info,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Notifications.Add(notification);
        await _dbContext.SaveChangesAsync();

        // Verify pre-condition: Database is InMemory
        Assert.Contains("InMemory", _dbContext.Database.ProviderName);

        // Act
        var result = await _identityService.DeleteUserAsync(user.Id);

        // Assert
        Assert.True(result.Succeeded, "DeleteUserAsync failed");

        // Clear change tracker to ensure we query the database, not the local cache
        _dbContext.ChangeTracker.Clear();

        // Verify User is gone
        var deletedUser = await _dbContext.Users.FindAsync(user.Id);
        Assert.Null(deletedUser);

        // Verify Notification is gone
        var deletedNotification = await _dbContext.Notifications.FindAsync(notification.Id);
        Assert.Null(deletedNotification);
    }
}
