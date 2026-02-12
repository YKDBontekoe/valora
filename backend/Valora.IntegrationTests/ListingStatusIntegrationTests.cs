using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Valora.Application.DTOs;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;
using Xunit;

namespace Valora.IntegrationTests;

[Collection("TestcontainersDatabase")]
public class ListingStatusIntegrationTests : IClassFixture<TestcontainersDatabaseFixture>, IAsyncLifetime
{
    private readonly IServiceScope _scope;
    protected readonly IntegrationTestWebAppFactory Factory;
    protected readonly ValoraDbContext DbContext;
    protected readonly HttpClient Client;

    public ListingStatusIntegrationTests(TestcontainersDatabaseFixture fixture)
    {
        Factory = fixture.Factory ?? throw new InvalidOperationException("Factory not initialized");
        Client = Factory.CreateClient();
        _scope = Factory.Services.CreateScope();
        DbContext = _scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
    }

    public virtual async Task InitializeAsync()
    {
        // Cleanup relevant tables
        DbContext.Listings.RemoveRange(DbContext.Listings);
        DbContext.RefreshTokens.RemoveRange(DbContext.RefreshTokens);
        DbContext.Notifications.RemoveRange(DbContext.Notifications);
        if (DbContext.Users.Any())
        {
            DbContext.Users.RemoveRange(DbContext.Users);
        }
        await DbContext.SaveChangesAsync();

        await AuthenticateAsync();
    }

    public Task DisposeAsync()
    {
        _scope?.Dispose();
        return Task.CompletedTask;
    }

    private async Task AuthenticateAsync(string email = "status_test@example.com", string password = "Password123!")
    {
        await Client.PostAsJsonAsync("/api/auth/register", new
        {
            Email = email,
            Password = password,
            ConfirmPassword = password
        });

        var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", new
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
        Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResponse.Token);
    }

    [Fact]
    public async Task GetListings_ShouldFilterOutInactiveListings_ByDefault()
    {
        // Arrange
        var activeListing = new Listing
        {
            FundaId = "Active1",
            Address = "Active Street 1",
            City = "Amsterdam",
            Price = 500000,
            ListedDate = DateTime.UtcNow,
            Status = "Beschikbaar",
            IsSoldOrRented = false
        };

        var soldListing = new Listing
        {
            FundaId = "Sold1",
            Address = "Sold Street 1",
            City = "Amsterdam",
            Price = 450000,
            ListedDate = DateTime.UtcNow.AddDays(-30),
            Status = "Verkocht",
            IsSoldOrRented = true
        };

        var withdrawnListing = new Listing
        {
            FundaId = "Withdrawn1",
            Address = "Withdrawn Street 1",
            City = "Amsterdam",
            Price = 600000,
            ListedDate = DateTime.UtcNow.AddDays(-60),
            Status = "Ingetrokken",
            IsSoldOrRented = false
        };

        var soldOrRentedTrueListing = new Listing
        {
            FundaId = "SoldOrRented1",
            Address = "SoldOrRented Street 1",
            City = "Amsterdam",
            Price = 550000,
            ListedDate = DateTime.UtcNow.AddDays(-10),
            Status = "Beschikbaar", // Even if status says available, the bool flag overrides
            IsSoldOrRented = true
        };

        DbContext.Listings.AddRange(activeListing, soldListing, withdrawnListing, soldOrRentedTrueListing);
        await DbContext.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync("/api/listings");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ListingStatusResponseDto>();

        Assert.NotNull(result);
        Assert.NotNull(result.Items);

        // This assertion currently FAILS because the API returns all listings
        // After the fix, it should PASS
        Assert.Single(result.Items);
        Assert.Equal("Active1", result.Items.First().FundaId);
    }
}

public class ListingStatusResponseDto
{
    public List<ListingSummaryDto> Items { get; set; } = new();
    public int PageIndex { get; set; }
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}
