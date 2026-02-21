using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Valora.Application.DTOs;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;
using Xunit;
using Shouldly;

namespace Valora.IntegrationTests;

[Collection("TestcontainersDatabase")]
public class AdminEndpointIntegrationTests : IAsyncLifetime
{
    private readonly TestcontainersDatabaseFixture _fixture;
    private IntegrationTestWebAppFactory _factory = null!;
    private HttpClient _client = null!;
    private ValoraDbContext _dbContext = null!;
    private IServiceScope _scope = null!;
    private const string AdminPassword = "AdminPassword123!";

    public AdminEndpointIntegrationTests(TestcontainersDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        // Use the shared factory from the fixture
        _factory = _fixture.Factory ?? throw new InvalidOperationException("Fixture factory not initialized");
        _client = _factory.CreateClient();
        _scope = _factory.Services.CreateScope();
        _dbContext = _scope.ServiceProvider.GetRequiredService<ValoraDbContext>();

        // Cleanup existing data
        _dbContext.Notifications.RemoveRange(_dbContext.Notifications);
        _dbContext.RefreshTokens.RemoveRange(_dbContext.RefreshTokens);
        if (_dbContext.Users.Any())
        {
            _dbContext.Users.RemoveRange(_dbContext.Users);
        }
        await _dbContext.SaveChangesAsync();
    }

    public Task DisposeAsync()
    {
        _scope?.Dispose();
        // Do not dispose _factory as it is managed by the fixture
        return Task.CompletedTask;
    }

    [Fact]
    public async Task DeleteUser_WhenSelfDeletion_ReturnsForbidden()
    {
        // Arrange
        var adminEmail = "admin_self@example.com";
        await AuthenticateAsAdminAsync(adminEmail);

        var adminUser = await _dbContext.Users.FirstAsync(u => u.Email == adminEmail);

        // Act
        var response = await _client.DeleteAsync($"/api/admin/users/{adminUser.Id}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteUser_WhenValidUser_ReturnsNoContent()
    {
        // Arrange
        await AuthenticateAsAdminAsync();

        var targetUser = new ApplicationUser
        {
            UserName = "target_user",
            Email = "target@example.com",
            EmailConfirmed = true
        };
        _dbContext.Users.Add(targetUser);
        await _dbContext.SaveChangesAsync();

        // Act
        var response = await _client.DeleteAsync($"/api/admin/users/{targetUser.Id}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        _dbContext.ChangeTracker.Clear();
        var deletedUser = await _dbContext.Users.FindAsync(targetUser.Id);
        deletedUser.ShouldBeNull();
    }

    [Fact]
    public async Task DeleteUser_WhenUserNotFound_ReturnsNotFound()
    {
        // Arrange
        await AuthenticateAsAdminAsync();

        // Act
        var response = await _client.DeleteAsync($"/api/admin/users/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetSystemStats_ReturnsStats()
    {
        // Arrange
        await AuthenticateAsAdminAsync();

        // Create a notification to have count > 0
        var user = await _dbContext.Users.FirstAsync(); // Admin user
        _dbContext.Notifications.Add(new Notification
        {
            UserId = user.Id,
            Title = "Test",
            Body = "Body",
            Type = NotificationType.Info
        });
        await _dbContext.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/api/admin/stats");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var stats = await response.Content.ReadFromJsonAsync<AdminStatsDto>();
        stats.ShouldNotBeNull();
        stats.TotalUsers.ShouldBe(1); // Only the admin user created in AuthenticateAsAdminAsync
        stats.TotalNotifications.ShouldBe(1); // Only the one we just added
    }

    private async Task AuthenticateAsAdminAsync(string email = "admin@example.com")
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            var result = await roleManager.CreateAsync(new IdentityRole("Admin"));
            if (!result.Succeeded) throw new Exception($"Failed to create Admin role: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
            var result = await userManager.CreateAsync(user, AdminPassword);
            if (!result.Succeeded) throw new Exception($"Failed to create admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        if (!await userManager.IsInRoleAsync(user, "Admin"))
        {
            var result = await userManager.AddToRoleAsync(user, "Admin");
            if (!result.Succeeded) throw new Exception($"Failed to add user to Admin role: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            Email = email,
            Password = AdminPassword
        });
        loginResponse.EnsureSuccessStatusCode();

        var authResponse = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
        if (authResponse?.Token == null)
        {
            throw new InvalidOperationException("Failed to extract auth token");
        }
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResponse.Token);
    }
}
