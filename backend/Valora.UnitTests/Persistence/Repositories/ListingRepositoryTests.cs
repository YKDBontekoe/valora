using Microsoft.EntityFrameworkCore;
using Valora.Application.DTOs;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;
using Valora.Infrastructure.Persistence.Repositories;
using Xunit;

namespace Valora.UnitTests.Persistence.Repositories;

public class ListingRepositoryTests
{
    private ValoraDbContext GetDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ValoraDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new ValoraDbContext(options);
    }

    [Fact]
    public async Task GetAllAsync_ShouldFilterBySearchTerm_HandlingNulls()
    {
        // Arrange
        using var context = GetDbContext(nameof(GetAllAsync_ShouldFilterBySearchTerm_HandlingNulls));
        context.Listings.AddRange(
            new Listing { FundaId = "1", Address = "Match Street", City = "City", PostalCode = "1234AB" },
            new Listing { FundaId = "2", Address = "Other Street", City = null, PostalCode = null }, // Nulls
            new Listing { FundaId = "3", Address = "Different", City = "City", PostalCode = "5678CD" }
        );
        await context.SaveChangesAsync();

        var repo = new ListingRepository(context);
        var filter = new ListingFilterDto { SearchTerm = "Match" };

        // Act
        var result = await repo.GetAllAsync(filter);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal("1", result.Items[0].FundaId);
    }

    [Fact]
    public async Task GetAllAsync_ShouldFilterByMinMaxPrice()
    {
        // Arrange
        using var context = GetDbContext(nameof(GetAllAsync_ShouldFilterByMinMaxPrice));
        context.Listings.AddRange(
            new Listing { FundaId = "1", Address = "A", Price = 100 },
            new Listing { FundaId = "2", Address = "B", Price = 200 },
            new Listing { FundaId = "3", Address = "C", Price = 300 }
        );
        await context.SaveChangesAsync();

        var repo = new ListingRepository(context);
        var filter = new ListingFilterDto { MinPrice = 150, MaxPrice = 250 };

        // Act
        var result = await repo.GetAllAsync(filter);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal("2", result.Items[0].FundaId);
    }

    [Fact]
    public async Task GetAllAsync_ShouldSortCorrectly()
    {
        // Arrange
        using var context = GetDbContext(nameof(GetAllAsync_ShouldSortCorrectly));
        context.Listings.AddRange(
            new Listing { FundaId = "1", Address = "A", Price = 100, ListedDate = DateTime.UtcNow.AddDays(-1) },
            new Listing { FundaId = "2", Address = "B", Price = 300, ListedDate = DateTime.UtcNow.AddDays(-3) },
            new Listing { FundaId = "3", Address = "C", Price = 200, ListedDate = DateTime.UtcNow.AddDays(-2) }
        );
        await context.SaveChangesAsync();

        var repo = new ListingRepository(context);

        // Price Desc
        var filterPriceDesc = new ListingFilterDto { SortBy = "Price", SortOrder = "desc" };
        var resultPriceDesc = await repo.GetAllAsync(filterPriceDesc);
        Assert.Equal("2", resultPriceDesc.Items[0].FundaId);
        Assert.Equal("3", resultPriceDesc.Items[1].FundaId);
        Assert.Equal("1", resultPriceDesc.Items[2].FundaId);

        // Date Asc
        var filterDateAsc = new ListingFilterDto { SortBy = "Date", SortOrder = "asc" };
        var resultDateAsc = await repo.GetAllAsync(filterDateAsc);
        Assert.Equal("2", resultDateAsc.Items[0].FundaId); // Oldest first
        Assert.Equal("3", resultDateAsc.Items[1].FundaId);
        Assert.Equal("1", resultDateAsc.Items[2].FundaId);
    }

    [Fact]
    public async Task CRUD_Operations_ShouldWork()
    {
        // Arrange
        using var context = GetDbContext(nameof(CRUD_Operations_ShouldWork));
        var repo = new ListingRepository(context);
        var listing = new Listing { FundaId = "1", Address = "New" };

        // Add
        await repo.AddAsync(listing);
        Assert.Equal(1, await context.Listings.CountAsync());

        // GetByFundaId
        var fetched = await repo.GetByFundaIdAsync("1");
        Assert.NotNull(fetched);

        // Update
        fetched!.Address = "Updated";
        await repo.UpdateAsync(fetched);
        var updated = await repo.GetByIdAsync(fetched.Id);
        Assert.Equal("Updated", updated!.Address);

        // Delete
        await repo.DeleteAsync(fetched.Id);
        Assert.Equal(0, await context.Listings.CountAsync());
    }
}
