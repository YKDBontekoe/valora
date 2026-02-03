using System.Net.Http.Json;
using Valora.Application.DTOs;
using Valora.Domain.Entities;
using Xunit;

namespace Valora.IntegrationTests;

public class ListingSearchPostgresTests : BasePostgresIntegrationTest
{
    public ListingSearchPostgresTests(TestcontainersDatabaseFixture fixture) : base(fixture)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await AuthenticateAsync();
    }

    [Fact]
    public async Task Search_Postgres_ILike_Matches_CaseInsensitive()
    {
        // Arrange
        var listing = new Listing
        {
            FundaId = "ILikeTest",
            Address = "UniquePostgresStreet",
            City = "PostgresCity",
            Price = 500000,
            ListedDate = DateTime.UtcNow
        };
        DbContext.Listings.Add(listing);
        await DbContext.SaveChangesAsync();

        // Act
        // ILike should match "postgrescity" to "PostgresCity"
        var response = await Client.GetAsync("/api/listings?city=postgrescity");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ListingResponseDto>();
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal("PostgresCity", result.Items.First().City);
    }

    [Fact]
    public async Task Search_Postgres_EscapeCharacters_Works()
    {
        // Arrange
        var listingWithPercent = new Listing
        {
            FundaId = "Escape1",
            Address = "100% Real Street",
            City = "EscapeCity",
            Price = 300000,
            ListedDate = DateTime.UtcNow
        };

        var listingWithUnderscore = new Listing
        {
            FundaId = "Escape2",
            Address = "Under_Score Street",
            City = "EscapeCity",
            Price = 300000,
            ListedDate = DateTime.UtcNow
        };

        var listingWithBackslash = new Listing
        {
            FundaId = "Escape3",
            Address = @"Back\Slash Street",
            City = "EscapeCity",
            Price = 300000,
            ListedDate = DateTime.UtcNow
        };

        DbContext.Listings.AddRange(listingWithPercent, listingWithUnderscore, listingWithBackslash);
        await DbContext.SaveChangesAsync();

        // Act & Assert 1: Search for "100%"
        // URL encode "%" as "%25"
        var response1 = await Client.GetAsync("/api/listings?searchTerm=100%25");
        response1.EnsureSuccessStatusCode();
        var result1 = await response1.Content.ReadFromJsonAsync<ListingResponseDto>();
        Assert.NotNull(result1);
        Assert.Single(result1.Items);
        Assert.Equal("100% Real Street", result1.Items.First().Address);

        // Act & Assert 2: Search for "Under_Score"
        var response2 = await Client.GetAsync("/api/listings?searchTerm=Under_Score");
        response2.EnsureSuccessStatusCode();
        var result2 = await response2.Content.ReadFromJsonAsync<ListingResponseDto>();
        Assert.NotNull(result2);
        Assert.Single(result2.Items);
        Assert.Equal("Under_Score Street", result2.Items.First().Address);

        // Act & Assert 3: Search for "Back\Slash"
        // URL encode "\" as "%5C"
        // Note: System.Net.WebUtility.UrlEncode or similar handles this, but here we just pass manual string.
        // We need to double encode if we were constructing manually maybe? No, HttpClient handles path/query somewhat but let's be explicit.
        // %5C is backslash.
        var response3 = await Client.GetAsync("/api/listings?searchTerm=Back%5CSlash");
        response3.EnsureSuccessStatusCode();
        var result3 = await response3.Content.ReadFromJsonAsync<ListingResponseDto>();
        Assert.NotNull(result3);
        Assert.Single(result3.Items);
        Assert.Equal(@"Back\Slash Street", result3.Items.First().Address);
    }

    [Fact]
    public async Task Search_Postgres_Wildcards_AreNotInterpreted()
    {
        // Arrange
        // "1005" contains "100". If searching for "100%" was interpreted as wildcard "100%", it would match "1005".
        // But we want "100%" to be treated as literal "100%".
        var target = new Listing
        {
            FundaId = "WildcardTarget",
            Address = "100% Valid",
            City = "WildcardCity",
            Price = 100,
            ListedDate = DateTime.UtcNow
        };

        var distractor = new Listing
        {
            FundaId = "WildcardDistractor",
            Address = "1005 Invalid",
            City = "WildcardCity",
            Price = 100,
            ListedDate = DateTime.UtcNow
        };

        DbContext.Listings.AddRange(target, distractor);
        await DbContext.SaveChangesAsync();

        // Act
        // Search for "100%"
        var response = await Client.GetAsync("/api/listings?searchTerm=100%25");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ListingResponseDto>();
        Assert.NotNull(result);

        // Should only match the literal "100%" one, not the one with "1005"
        Assert.Single(result.Items);
        Assert.Equal("100% Valid", result.Items.First().Address);
    }
}
