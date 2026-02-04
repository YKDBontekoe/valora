using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Valora.Application.DTOs;
using Valora.Infrastructure.Persistence;

namespace Valora.IntegrationTests.Endpoints;

public class ScraperEndpointsTests : IDisposable
{
    private readonly IntegrationTestWebAppFactory _factory;
    private readonly HttpClient _client;
    private readonly IServiceScope _scope;
    private readonly ValoraDbContext _dbContext;

    public ScraperEndpointsTests()
    {
        _factory = new IntegrationTestWebAppFactory("InMemory");
        _client = _factory.CreateClient();
        _scope = _factory.Services.CreateScope();
        _dbContext = _scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
        _dbContext.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _scope.Dispose();
        _factory.Dispose();
    }

    private async Task AuthenticateAsync()
    {
        // Register and login to get token
        var registerDto = new RegisterDto { Email = "admin@example.com", Password = "Password123!", ConfirmPassword = "Password123!" };
        await _client.PostAsJsonAsync("/api/auth/register", registerDto);

        var loginDto = new LoginDto("admin@example.com", "Password123!");
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);
        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponseDto>();

        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResponse!.Token);
    }

    [Fact]
    public async Task TriggerScraper_WhenHangfireDisabled_ShouldReturn503()
    {
        await AuthenticateAsync();

        // Act
        var response = await _client.PostAsync("/api/scraper/trigger", null);

        // Assert
        // The factory sets HANGFIRE_ENABLED = false by default
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task TriggerLimitedScraper_WhenHangfireDisabled_ShouldReturn503()
    {
        await AuthenticateAsync();

        // Act
        var response = await _client.PostAsync("/api/scraper/trigger-limited?region=amsterdam&limit=10", null);

        // Assert
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task Seed_WhenRegionMissing_ShouldReturnBadRequest()
    {
        await AuthenticateAsync();

        // Act
        var response = await _client.PostAsync("/api/scraper/seed", null);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
