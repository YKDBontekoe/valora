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
    public async Task GetAllAsync_ShouldFilterByDetails()
    {
        // Arrange
        using var context = new ValoraDbContext(_options);
        context.Listings.AddRange(
            new Listing { FundaId = "1", Address = "Small", Bedrooms = 1, LivingAreaM2 = 50, CreatedAt = DateTime.UtcNow },
            new Listing { FundaId = "2", Address = "Medium", Bedrooms = 2, LivingAreaM2 = 100, CreatedAt = DateTime.UtcNow },
            new Listing { FundaId = "3", Address = "Large", Bedrooms = 4, LivingAreaM2 = 200, CreatedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var repository = new ListingRepository(context);

        // Filter by Bedrooms
        var resBedrooms = await repository.GetAllAsync(new ListingFilterDto { MinBedrooms = 2 });
        Assert.Equal(2, resBedrooms.TotalCount);
        Assert.Contains(resBedrooms.Items, l => l.Address == "Medium");
        Assert.Contains(resBedrooms.Items, l => l.Address == "Large");

        // Filter by Min Living Area
        var resMinArea = await repository.GetAllAsync(new ListingFilterDto { MinLivingArea = 150 });
        Assert.Single(resMinArea.Items);
        Assert.Equal("Large", resMinArea.Items[0].Address);

        // Filter by Max Living Area
        var resMaxArea = await repository.GetAllAsync(new ListingFilterDto { MaxLivingArea = 80 });
        Assert.Single(resMaxArea.Items);
        Assert.Equal("Small", resMaxArea.Items[0].Address);

        // Filter by Living Area Range
        var resAreaRange = await repository.GetAllAsync(new ListingFilterDto { MinLivingArea = 80, MaxLivingArea = 120 });
        Assert.Single(resAreaRange.Items);
        Assert.Equal("Medium", resAreaRange.Items[0].Address);
    }

    [Fact]
    public async Task GetActiveListingsAsync_ShouldReturnOnlyActiveListings()
    {
        // Arrange
        using var context = new ValoraDbContext(_options);
        context.Listings.AddRange(
            new Listing { FundaId = "1", Address = "Active 1", Status = "Beschikbaar", CreatedAt = DateTime.UtcNow },
            new Listing { FundaId = "2", Address = "Sold", Status = "Verkocht", CreatedAt = DateTime.UtcNow },
            new Listing { FundaId = "3", Address = "Withdrawn", Status = "Ingetrokken", CreatedAt = DateTime.UtcNow },
            new Listing { FundaId = "4", Address = "Active 2", Status = "Onder bod", CreatedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var repository = new ListingRepository(context);

        // Act
        var result = await repository.GetActiveListingsAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, l => l.Address == "Active 1");
        Assert.Contains(result, l => l.Address == "Active 2");
        Assert.DoesNotContain(result, l => l.Address == "Sold");
        Assert.DoesNotContain(result, l => l.Address == "Withdrawn");
    }
}
