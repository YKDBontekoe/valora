using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Valora.Application.DTOs;
using Valora.Infrastructure.Persistence;

namespace Valora.IntegrationTests.Endpoints;

public class SearchEndpointsTests : IDisposable
{
    private readonly IntegrationTestWebAppFactory _factory;
    private readonly HttpClient _client;
    private readonly IServiceScope _scope;
    private readonly ValoraDbContext _dbContext;

    public SearchEndpointsTests()
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
        var registerDto = new RegisterDto { Email = "user@example.com", Password = "Password123!", ConfirmPassword = "Password123!" };
        await _client.PostAsJsonAsync("/api/auth/register", registerDto);

        var loginDto = new LoginDto("user@example.com", "Password123!");
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);
        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponseDto>();

        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResponse!.Token);
    }

    [Fact]
    public async Task Search_WhenRegionMissing_ShouldReturnBadRequest()
    {
        await AuthenticateAsync();

        // Act
        var response = await _client.GetAsync("/api/search?Region=");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Lookup_WhenUrlMissing_ShouldReturnBadRequest()
    {
        await AuthenticateAsync();

        // Act
        var response = await _client.GetAsync("/api/lookup");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
