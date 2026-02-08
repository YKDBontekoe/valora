using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;
using Xunit;

namespace Valora.IntegrationTests;

[Collection("TestcontainersDatabase")]
public class TestcontainersIntegrationTest : IAsyncLifetime
{
    private readonly IServiceScope _scope;
    protected readonly IntegrationTestWebAppFactory Factory;
    protected readonly ValoraDbContext DbContext;
    protected readonly HttpClient Client;

    public TestcontainersIntegrationTest(TestcontainersDatabaseFixture fixture)
    {
        Factory = fixture.Factory ?? throw new InvalidOperationException("Factory not initialized");
        Client = Factory.CreateClient();
        _scope = Factory.Services.CreateScope();
        DbContext = _scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
    }

    public virtual async Task InitializeAsync()
    {
        // Simple cleanup of Notification table before each test
        if (DbContext.Notifications.Any())
        {
            DbContext.Notifications.RemoveRange(DbContext.Notifications);
        }
        if (DbContext.RefreshTokens.Any())
        {
            DbContext.RefreshTokens.RemoveRange(DbContext.RefreshTokens);
        }
        if (DbContext.Users.Any())
        {
            DbContext.Users.RemoveRange(DbContext.Users);
        }
        await DbContext.SaveChangesAsync();
    }

    protected async Task AuthenticateAsync(string email = "test@example.com", string password = "Password123!")
    {
        var registerResponse = await Client.PostAsJsonAsync("/api/auth/register", new
        {
            Email = email,
            Password = password,
            ConfirmPassword = password
        });
        // We allow register to fail (e.g. user already exists) but login must succeed

        var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            Email = email,
            Password = password
        });
        loginResponse.EnsureSuccessStatusCode();

        var authResponse = await loginResponse.Content.ReadFromJsonAsync<Valora.Application.DTOs.AuthResponseDto>();
        if (authResponse?.Token == null)
        {
            throw new InvalidOperationException("Failed to extract auth token from login response");
        }
        Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResponse.Token);
    }

    public virtual Task DisposeAsync()
    {
        _scope?.Dispose();
        return Task.CompletedTask;
    }

    protected T GetRequiredService<T>() where T : notnull
    {
        return _scope.ServiceProvider.GetRequiredService<T>();
    }
}
