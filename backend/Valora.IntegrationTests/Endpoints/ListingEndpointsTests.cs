using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Valora.Application.DTOs;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;

namespace Valora.IntegrationTests.Endpoints;

public class ListingEndpointsTests : IDisposable
{
    private readonly IntegrationTestWebAppFactory _factory;
    private readonly HttpClient _client;
    private readonly IServiceScope _scope;
    private readonly ValoraDbContext _dbContext;

    public ListingEndpointsTests()
    {
        // Manually instantiate factory
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
        var registerDto = new RegisterDto { Email = "test@example.com", Password = "Password123!", ConfirmPassword = "Password123!" };
        await _client.PostAsJsonAsync("/api/auth/register", registerDto);

        var loginDto = new LoginDto("test@example.com", "Password123!");
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);
        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponseDto>();

        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResponse!.Token);
    }

    [Fact]
    public async Task GetListings_ShouldReturnOk_WithPaginatedList()
    {
        await AuthenticateAsync();

        // Arrange
        var listing = new Listing
        {
            Id = Guid.NewGuid(),
            FundaId = "123",
            Address = "Test Address",
            City = "Test City",
            Price = 500000,
            CreatedAt = DateTime.UtcNow,
            ImageUrls = new List<string>(),
            Features = new Dictionary<string, string>(),
            Labels = new List<string>(),
            FloorPlanUrls = new List<string>()
        };
        _dbContext.Listings.Add(listing);
        await _dbContext.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/api/listings");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadFromJsonAsync<dynamic>();
        Assert.NotNull(content);
    }

    [Fact]
    public async Task GetListingById_WhenExists_ShouldReturnOk()
    {
        await AuthenticateAsync();

        // Arrange
        var id = Guid.NewGuid();
        var listing = new Listing
        {
            Id = id,
            FundaId = "456",
            Address = "Test Address 2",
            City = "Test City 2",
            Price = 600000,
            CreatedAt = DateTime.UtcNow,
            ImageUrls = new List<string>(),
            Features = new Dictionary<string, string>(),
            Labels = new List<string>(),
            FloorPlanUrls = new List<string>()
        };
        _dbContext.Listings.Add(listing);
        await _dbContext.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync($"/api/listings/{id}");

        // Assert
        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<ListingDto>();
        Assert.NotNull(dto);
        Assert.Equal(id, dto!.Id);
    }

    [Fact]
    public async Task GetListingById_WhenNotExists_ShouldReturnNotFound()
    {
        await AuthenticateAsync();

        // Act
        var response = await _client.GetAsync($"/api/listings/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
