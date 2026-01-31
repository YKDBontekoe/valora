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
            new Listing { FundaId = "1", Address = "Main Street", City = "Amsterdam", CreatedAt = DateTime.UtcNow },
            new Listing { FundaId = "2", Address = "Side Street", City = "Rotterdam", CreatedAt = DateTime.UtcNow },
            new Listing { FundaId = "3", Address = "Back Alley", City = "Utrecht", PostalCode = "1234AB", CreatedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var repository = new ListingRepository(context);
        var filter = new ListingFilterDto { SearchTerm = "Street" };

        // Act
        var result = await repository.GetAllAsync(filter);

        // Assert
        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, l => Assert.Contains("Street", l.Address));
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
        var filter = new ListingFilterDto { MinPrice = 150000, MaxPrice = 250000 };

        // Act
        var result = await repository.GetAllAsync(filter);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal(200000, result.Items[0].Price);
    }

    [Fact]
    public async Task GetAllAsync_ShouldSortByPrice()
    {
        // Arrange
        using var context = new ValoraDbContext(_options);
        context.Listings.AddRange(
            new Listing { FundaId = "1", Address = "A", Price = 300000, CreatedAt = DateTime.UtcNow },
            new Listing { FundaId = "2", Address = "B", Price = 100000, CreatedAt = DateTime.UtcNow },
            new Listing { FundaId = "3", Address = "C", Price = 200000, CreatedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var repository = new ListingRepository(context);
        var filterAsc = new ListingFilterDto { SortBy = "price", SortOrder = "asc" };
        var filterDesc = new ListingFilterDto { SortBy = "price", SortOrder = "desc" };

        // Act
        var resultAsc = await repository.GetAllAsync(filterAsc);
        var resultDesc = await repository.GetAllAsync(filterDesc);

        // Assert
        Assert.Equal(100000, resultAsc.Items[0].Price);
        Assert.Equal(300000, resultDesc.Items[0].Price);
    }

    [Fact]
    public async Task GetAllAsync_ShouldSortByDate()
    {
        // Arrange
        var date1 = DateTime.UtcNow.AddDays(-2);
        var date2 = DateTime.UtcNow.AddDays(-1);
        var date3 = DateTime.UtcNow;

        using var context = new ValoraDbContext(_options);
        context.Listings.AddRange(
            new Listing { FundaId = "1", Address = "A", ListedDate = date1, CreatedAt = DateTime.UtcNow },
            new Listing { FundaId = "2", Address = "B", ListedDate = date2, CreatedAt = DateTime.UtcNow },
            new Listing { FundaId = "3", Address = "C", ListedDate = date3, CreatedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var repository = new ListingRepository(context);
        var filterAsc = new ListingFilterDto { SortBy = "date", SortOrder = "asc" };
        var filterDesc = new ListingFilterDto { SortBy = "date", SortOrder = "desc" };

        // Act
        var resultAsc = await repository.GetAllAsync(filterAsc);
        var resultDesc = await repository.GetAllAsync(filterDesc);

        // Assert
        Assert.Equal("A", resultAsc.Items[0].Address); // Oldest first
        Assert.Equal("C", resultDesc.Items[0].Address); // Newest first
    }
}
