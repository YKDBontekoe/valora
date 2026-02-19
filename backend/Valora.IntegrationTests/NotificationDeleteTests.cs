using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Valora.Application.DTOs;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;
using Xunit;

namespace Valora.IntegrationTests;

[Collection("TestcontainersDatabase")]
public class NotificationDeleteTests : IAsyncLifetime
{
    private readonly TestcontainersDatabaseFixture _fixture;
    private readonly IntegrationTestWebAppFactory _factory;
    private readonly ValoraDbContext _dbContext;
    private readonly HttpClient _client;
    private IServiceScope? _scope;

    public NotificationDeleteTests(TestcontainersDatabaseFixture fixture)
    {
        _fixture = fixture;
        _factory = fixture.Factory ?? throw new InvalidOperationException("Factory not initialized");
        _client = _factory.CreateClient();
        _scope = _factory.Services.CreateScope();
        _dbContext = _scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
    }

    public async Task InitializeAsync()
    {
        // Cleanup existing notifications and users to ensure test isolation
        _dbContext.Notifications.RemoveRange(_dbContext.Notifications);
        if (_dbContext.Users.Any())
        {
            _dbContext.Users.RemoveRange(_dbContext.Users);
        }
        await _dbContext.SaveChangesAsync();
    }

    public Task DisposeAsync()
    {
        _scope?.Dispose();
        return Task.CompletedTask;
    }

    private async Task AuthenticateAsync(string email = "test@example.com", string password = "Password123!")
    {
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            Email = email,
            Password = password,
            ConfirmPassword = password
        });
        // We allow register to fail (e.g. user already exists) but login must succeed

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            Email = email,
            Password = password
        });
        loginResponse.EnsureSuccessStatusCode();

        var authResponse = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
        if (authResponse?.Token == null)
        {
            throw new InvalidOperationException("Failed to extract auth token from login response");
        }
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResponse.Token);
    }

    [Fact]
    public async Task Delete_OwnNotification_ReturnsNoContent_AndRemovesFromDb()
    {
        // Arrange
        var email = "owner@example.com";
        await AuthenticateAsync(email);
        var user = await _dbContext.Users.FirstAsync(u => u.Email == email);

        var notification = new Notification
        {
            UserId = user.Id,
            Title = "My Notification",
            Body = "Body",
            Type = NotificationType.Info,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Notifications.Add(notification);
        await _dbContext.SaveChangesAsync();

        // Act
        var response = await _client.DeleteAsync($"/api/notifications/{notification.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        _dbContext.ChangeTracker.Clear();
        var exists = await _dbContext.Notifications.AnyAsync(n => n.Id == notification.Id);
        Assert.False(exists, "Notification should be deleted from DB");
    }

    [Fact]
    public async Task Delete_OtherUserNotification_ReturnsNotFound_AndDoesNotRemove()
    {
        // Arrange
        var ownerEmail = "victim@example.com";
        var attackerEmail = "attacker@example.com";

        // Create owner and notification
        await AuthenticateAsync(ownerEmail);
        var owner = await _dbContext.Users.FirstAsync(u => u.Email == ownerEmail);

        var notification = new Notification
        {
            UserId = owner.Id,
            Title = "Victim Notification",
            Body = "Secret",
            Type = NotificationType.Info,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Notifications.Add(notification);
        await _dbContext.SaveChangesAsync();

        // Authenticate as attacker
        await AuthenticateAsync(attackerEmail); // This switches the auth token to attacker

        // Act
        var response = await _client.DeleteAsync($"/api/notifications/{notification.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode); // Should mask existence/forbidden as NotFound per implementation

        _dbContext.ChangeTracker.Clear();
        var exists = await _dbContext.Notifications.AnyAsync(n => n.Id == notification.Id);
        Assert.True(exists, "Notification should NOT be deleted");
    }

    [Fact]
    public async Task Delete_NonExistentNotification_ReturnsNotFound()
    {
        // Arrange
        await AuthenticateAsync();

        // Act
        var response = await _client.DeleteAsync($"/api/notifications/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_Unauthenticated_ReturnsUnauthorized()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await _client.DeleteAsync($"/api/notifications/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
