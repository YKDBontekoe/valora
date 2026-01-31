using Microsoft.EntityFrameworkCore;
using Valora.Application.DTOs;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;
using Valora.Infrastructure.Persistence.Repositories;
using Xunit;

namespace Valora.UnitTests.Repositories;

public class ListingRepositoryTests
{
    private readonly DbContextOptions<ValoraDbContext> _options;

    public ListingRepositoryTests()
    {
        _options = new DbContextOptionsBuilder<ValoraDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task GetAllAsync_ShouldFilterBySearchTerm()
    {
        // Arrange
        using var context = new ValoraDbContext(_options);
        context.Listings.AddRange(
            new Listing { FundaId = "1", Address = "Main Street", City = "Amsterdam", PostalCode = "1000AA", CreatedAt = DateTime.UtcNow },
            new Listing { FundaId = "2", Address = "Side Street", City = "Rotterdam", PostalCode = "2000BB", CreatedAt = DateTime.UtcNow },
            new Listing { FundaId = "3", Address = "Back Alley", City = "Utrecht", PostalCode = "1234AB", CreatedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var repository = new ListingRepository(context);

        // Act & Assert 1: Address match
        var result1 = await repository.GetAllAsync(new ListingFilterDto { SearchTerm = "Main" });
        Assert.Single(result1.Items);
        Assert.Equal("Main Street", result1.Items[0].Address);

        // Act & Assert 2: City match
        var result2 = await repository.GetAllAsync(new ListingFilterDto { SearchTerm = "Rotterdam" });
        Assert.Single(result2.Items);
        Assert.Equal("Side Street", result2.Items[0].Address);

        // Act & Assert 3: PostalCode match
        var result3 = await repository.GetAllAsync(new ListingFilterDto { SearchTerm = "1234" });
        Assert.Single(result3.Items);
        Assert.Equal("Back Alley", result3.Items[0].Address);
    }

    [Fact]
    public async Task GetAllAsync_ShouldFilterByPriceRange()
    {
        // Arrange
        using var context = new ValoraDbContext(_options);
        context.Listings.AddRange(
            new Listing { FundaId = "1", Address = "A", Price = 100000, CreatedAt = DateTime.UtcNow },
            new Listing { FundaId = "2", Address = "B", Price = 200000, CreatedAt = DateTime.UtcNow },
            new Listing { FundaId = "3", Address = "C", Price = 300000, CreatedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var repository = new ListingRepository(context);

        // Min only
        var resultMin = await repository.GetAllAsync(new ListingFilterDto { MinPrice = 250000 });
        Assert.Single(resultMin.Items);
        Assert.Equal(300000, resultMin.Items[0].Price);

        // Max only
        var resultMax = await repository.GetAllAsync(new ListingFilterDto { MaxPrice = 150000 });
        Assert.Single(resultMax.Items);
        Assert.Equal(100000, resultMax.Items[0].Price);

        // Range
        var resultRange = await repository.GetAllAsync(new ListingFilterDto { MinPrice = 150000, MaxPrice = 250000 });
        Assert.Single(resultRange.Items);
        Assert.Equal(200000, resultRange.Items[0].Price);
    }

    [Fact]
    public async Task GetAllAsync_ShouldFilterByCity()
    {
        // Arrange
        using var context = new ValoraDbContext(_options);
        context.Listings.AddRange(
            new Listing { FundaId = "1", Address = "A", City = "Amsterdam", CreatedAt = DateTime.UtcNow },
            new Listing { FundaId = "2", Address = "B", City = "Rotterdam", CreatedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var repository = new ListingRepository(context);

        var result = await repository.GetAllAsync(new ListingFilterDto { City = "amsterdam" }); // Case insensitive check
        Assert.Single(result.Items);
        Assert.Equal("A", result.Items[0].Address);
    }

    [Fact]
    public async Task GetAllAsync_ShouldSortCorrectly()
    {
        // Arrange
        var date1 = DateTime.UtcNow.AddDays(-2);
        var date2 = DateTime.UtcNow.AddDays(-1);
        var date3 = DateTime.UtcNow;

        using var context = new ValoraDbContext(_options);
        context.Listings.AddRange(
            new Listing { FundaId = "1", Address = "A", Price = 300000, ListedDate = date1, CreatedAt = DateTime.UtcNow },
            new Listing { FundaId = "2", Address = "B", Price = 100000, ListedDate = date2, CreatedAt = DateTime.UtcNow },
            new Listing { FundaId = "3", Address = "C", Price = 200000, ListedDate = date3, CreatedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var repository = new ListingRepository(context);

        // Price Asc
        var resPriceAsc = await repository.GetAllAsync(new ListingFilterDto { SortBy = "price", SortOrder = "asc" });
        Assert.Equal(100000, resPriceAsc.Items[0].Price);

        // Price Desc
        var resPriceDesc = await repository.GetAllAsync(new ListingFilterDto { SortBy = "price", SortOrder = "desc" });
        Assert.Equal(300000, resPriceDesc.Items[0].Price);

        // Date Asc
        var resDateAsc = await repository.GetAllAsync(new ListingFilterDto { SortBy = "date", SortOrder = "asc" });
        Assert.Equal("A", resDateAsc.Items[0].Address);

        // Date Desc (Default)
        var resDateDesc = await repository.GetAllAsync(new ListingFilterDto { SortBy = "date", SortOrder = "desc" });
        Assert.Equal("C", resDateDesc.Items[0].Address);

        // Default (Null sort)
        var resDefault = await repository.GetAllAsync(new ListingFilterDto());
        Assert.Equal("C", resDefault.Items[0].Address);
    }

    [Fact]
    public async Task GetAllAsync_ShouldHandleNullFields_WhenFiltering()
    {
        // Arrange
        using var context = new ValoraDbContext(_options);
        context.Listings.AddRange(
            new Listing { FundaId = "1", Address = "Null City", City = null, PostalCode = null, CreatedAt = DateTime.UtcNow },
            new Listing { FundaId = "2", Address = "Null Postal", City = "Amsterdam", PostalCode = null, CreatedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var repository = new ListingRepository(context);

        // Act 1: Search term that usually checks City/PostalCode
        // Should not throw and should find by address
        var result1 = await repository.GetAllAsync(new ListingFilterDto { SearchTerm = "Null" });
        Assert.Equal(2, result1.Items.Count);

        // Act 2: Search term that matches nothing but would check null fields
        var result2 = await repository.GetAllAsync(new ListingFilterDto { SearchTerm = "NonExistent" });
        Assert.Empty(result2.Items);

        // Act 3: Filter by specific City (should filter out nulls)
        var result3 = await repository.GetAllAsync(new ListingFilterDto { City = "Amsterdam" });
        Assert.Single(result3.Items);
        Assert.Equal("Null Postal", result3.Items[0].Address);
    }
}
