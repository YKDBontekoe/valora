using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Valora.Application.Common.Interfaces;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;
using Xunit;

namespace Valora.IntegrationTests;

[Collection("TestcontainersDatabase")]
public class IdentityServiceIntegrationTests : IAsyncLifetime
{
    private readonly TestcontainersDatabaseFixture _fixture;
    private IntegrationTestWebAppFactory _factory = null!;
    private IServiceScope _scope = null!;
    private IIdentityService _identityService = null!;
    private ValoraDbContext _dbContext = null!;

    public IdentityServiceIntegrationTests(TestcontainersDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _factory = new IntegrationTestWebAppFactory(_fixture.ConnectionString);
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
}
