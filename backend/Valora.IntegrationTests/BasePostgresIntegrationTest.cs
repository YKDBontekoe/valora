using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Valora.Infrastructure.Persistence;
using Xunit;

namespace Valora.IntegrationTests;

[Collection("TestcontainersDatabase")]
public class BasePostgresIntegrationTest : IAsyncLifetime
{
    private readonly IServiceScope _scope;
    protected readonly IntegrationTestWebAppFactory Factory;
    protected readonly ValoraDbContext DbContext;
    protected readonly HttpClient Client;

    public BasePostgresIntegrationTest(TestcontainersDatabaseFixture fixture)
    {
        Factory = fixture.Factory ?? throw new InvalidOperationException("Factory not initialized");
        Client = Factory.CreateClient();
        _scope = Factory.Services.CreateScope();
        DbContext = _scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
    }

    public virtual async Task InitializeAsync()
    {
        // Efficient cleanup using ExecuteDeleteAsync
        await DbContext.Listings.ExecuteDeleteAsync();
        await DbContext.RefreshTokens.ExecuteDeleteAsync();

        // IdentityUser tables are linked.
        if (await DbContext.Users.AnyAsync())
        {
            await DbContext.Users.ExecuteDeleteAsync();
        }
    }

    protected async Task AuthenticateAsync()
    {
        var registerResponse = await Client.PostAsJsonAsync("/api/auth/register", new
        {
            Email = "test@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        });
        registerResponse.EnsureSuccessStatusCode();

        var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            Email = "test@example.com",
            Password = "Password123!"
        });
        loginResponse.EnsureSuccessStatusCode();

        var authResponse = await loginResponse.Content.ReadFromJsonAsync<Valora.Application.DTOs.AuthResponseDto>();
        if (authResponse != null)
        {
            Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResponse.Token);
        }
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
