using System.Net.Http.Json;
using Valora.Application.DTOs;
using Valora.Domain.Entities;
using Xunit;

namespace Valora.IntegrationTests;

public class ListingSearchIntegrationTests : BaseIntegrationTest
{
    public ListingSearchIntegrationTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await AuthenticateAsync();
    }

    [Fact]
    public async Task Search_CaseInsensitive_Matches()
    {
        // Arrange
        var listing = new Listing
        {
            FundaId = "Search1",
            Address = "Test Street 1",
            City = "Amsterdam",
            Price = 500000,
            ListedDate = DateTime.UtcNow
        };
        DbContext.Listings.Add(listing);
        await DbContext.SaveChangesAsync();

        // Act
        // Search with lowercase "amsterdam" should match "Amsterdam"
        var response = await Client.GetAsync("/api/listings?city=amsterdam");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ListingResponseDto>();
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal("Amsterdam", result.Items.First().City);
    }

    [Fact]
    public async Task Search_PartialMatch_Works()
    {
        // Arrange
        var listing = new Listing
        {
            FundaId = "Search2",
            Address = "Test Street 2",
            City = "Rotterdam",
            Price = 400000,
            ListedDate = DateTime.UtcNow
        };
        DbContext.Listings.Add(listing);
        await DbContext.SaveChangesAsync();

        // Act
        // Search "Rotter" should match "Rotterdam"
        var response = await Client.GetAsync("/api/listings?searchTerm=Rotter");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ListingResponseDto>();
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal("Rotterdam", result.Items.First().City);
    }

    [Fact]
    public async Task Search_SpecialCharacters_AreHandled()
    {
        // Arrange
        // Create a listing with a special character usually reserved for SQL wildcards
        var listing1 = new Listing
        {
            FundaId = "Search3",
            Address = "100% Real Street",
            City = "Utrecht",
            Price = 300000,
            ListedDate = DateTime.UtcNow
        };
        // Create a distractor that would be matched if "100%" was treated as a wildcard "%100%%"
        // If unescaped, "100%" becomes LIKE '%100%%', which matches anything containing "100"
        var listing2 = new Listing
        {
            FundaId = "Distractor",
            Address = "1005 Fake Street",
            City = "Utrecht",
            Price = 300000,
            ListedDate = DateTime.UtcNow
        };

        DbContext.Listings.AddRange(listing1, listing2);
        await DbContext.SaveChangesAsync();

        // Act
        // Search for "100%"
        // We expect the backend to handle this as a literal search, not a wildcard.
        // In InMemory: .Contains("100%") works.
        // In Postgres: The code escapes it to "100\%" for ILike, so it works.
        var response = await Client.GetAsync("/api/listings?searchTerm=100%25");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ListingResponseDto>();
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal("100% Real Street", result.Items.First().Address);
    }

    [Fact]
    public async Task Search_NoResults_ReturnsEmptyList()
    {
        // Arrange
        var listing = new Listing
        {
            FundaId = "Search4",
            Address = "Hidden Place",
            City = "Nowhere",
            Price = 100000,
            ListedDate = DateTime.UtcNow
        };
        DbContext.Listings.Add(listing);
        await DbContext.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync("/api/listings?searchTerm=NonExistent");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ListingResponseDto>();
        Assert.NotNull(result);
        Assert.Empty(result.Items);
    }
}
