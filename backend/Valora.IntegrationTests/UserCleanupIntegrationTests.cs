using System;
using System.Linq;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Valora.Application.Common.Interfaces;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;
using Xunit;

namespace Valora.IntegrationTests;

[Collection("TestcontainersDatabase")]
public class UserCleanupIntegrationTests : IAsyncLifetime
{
    private readonly TestcontainersDatabaseFixture _fixture;
    private IntegrationTestWebAppFactory _factory = null!;
    private IServiceScope _scope = null!;
    private ValoraDbContext _dbContext = null!;
    private IIdentityService _identityService = null!;

    public UserCleanupIntegrationTests(TestcontainersDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _factory = new IntegrationTestWebAppFactory(_fixture.ConnectionString);
        _scope = _factory.Services.CreateScope();
        _dbContext = _scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
        _identityService = _scope.ServiceProvider.GetRequiredService<IIdentityService>();

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
    public async Task DeleteUser_ShouldRemoveDependentNotifications_WhenUserIsDeleted()
    {
        // Arrange
        var user = new ApplicationUser
        {
            UserName = "notif_user",
            Email = "notif@example.com",
            EmailConfirmed = true
        };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var notification = new Notification
        {
            UserId = user.Id,
            Title = "Orphan Test",
            Body = "This should be deleted",
            Type = NotificationType.Info,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Notifications.Add(notification);
        await _dbContext.SaveChangesAsync();

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

    [Fact]
    public async Task DeleteUser_ShouldRemoveDependentRefreshTokens_WhenUserIsDeleted()
    {
        // Arrange
        var user = new ApplicationUser
        {
            UserName = "token_user",
            Email = "token@example.com",
            EmailConfirmed = true
        };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var token = new RefreshToken
        {
            TokenHash = "hash123",
            UserId = user.Id,
            Expires = DateTime.UtcNow.AddDays(7)
        };
        _dbContext.RefreshTokens.Add(token);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _identityService.DeleteUserAsync(user.Id);

        // Assert
        Assert.True(result.Succeeded, "DeleteUserAsync failed");

        // Clear change tracker to ensure we query the database, not the local cache
        _dbContext.ChangeTracker.Clear();

        // Verify User is gone
        var deletedUser = await _dbContext.Users.FindAsync(user.Id);
        Assert.Null(deletedUser);

        // Verify Token is gone (Cascade delete check)
        var deletedToken = await _dbContext.RefreshTokens.FindAsync(token.Id);
        Assert.Null(deletedToken);
    }
}
