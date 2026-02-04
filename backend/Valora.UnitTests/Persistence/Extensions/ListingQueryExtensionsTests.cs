using Valora.Application.DTOs;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence.Extensions;

namespace Valora.UnitTests.Persistence.Extensions;

public class ListingQueryExtensionsTests
{
    private readonly IQueryable<Listing> _listings;

    public ListingQueryExtensionsTests()
    {
        _listings = new List<Listing>
        {
            new() { Id = Guid.NewGuid(), FundaId = "1", Address = "Main St 1", City = "New York", Price = 500000, ListedDate = DateTime.UtcNow.AddDays(-1), LivingAreaM2 = 100 },
            new() { Id = Guid.NewGuid(), FundaId = "2", Address = "Broadway 2", City = "New York", Price = 1000000, ListedDate = DateTime.UtcNow, LivingAreaM2 = 200 },
            new() { Id = Guid.NewGuid(), FundaId = "3", Address = "Side St 3", City = "Boston", Price = 300000, ListedDate = DateTime.UtcNow.AddDays(-5), LivingAreaM2 = 80 },
            new() { Id = Guid.NewGuid(), FundaId = "4", Address = "High St 4", City = null, Price = 400000, ListedDate = DateTime.UtcNow.AddDays(-2), LivingAreaM2 = 120 }
        }.AsQueryable();
    }

    [Fact]
    public void ApplySearchFilter_Generic_FiltersCorrectly()
    {
        var filter = new ListingFilterDto { SearchTerm = "Main" };
        var result = _listings.ApplySearchFilter(filter, isPostgres: false).ToList();

        Assert.Single(result);
        Assert.Equal("Main St 1", result[0].Address);
    }

    [Fact]
    public void ApplySearchFilter_Generic_FiltersByCity()
    {
        var filter = new ListingFilterDto { SearchTerm = "Boston" };
        var result = _listings.ApplySearchFilter(filter, isPostgres: false).ToList();

        Assert.Single(result);
        Assert.Equal("Side St 3", result[0].Address);
    }

    [Fact]
    public void ApplySearchFilter_EmptyTerm_ReturnsAll()
    {
        var filter = new ListingFilterDto { SearchTerm = "" };
        var result = _listings.ApplySearchFilter(filter, isPostgres: false).ToList();

        Assert.Equal(4, result.Count);
    }

    [Fact]
    public void ApplyCityFilter_Generic_FiltersCorrectly()
    {
        var result = _listings.ApplyCityFilter("New York", isPostgres: false).ToList();

        Assert.Equal(2, result.Count);
        Assert.All(result, l => Assert.Equal("New York", l.City));
    }

    [Fact]
    public void ApplyCityFilter_EmptyCity_ReturnsAll()
    {
        var result = _listings.ApplyCityFilter("", isPostgres: false).ToList();

        Assert.Equal(4, result.Count);
    }

    [Fact]
    public void ApplySorting_PriceAsc()
    {
        var result = _listings.ApplySorting("price", "asc").ToList();
        Assert.Equal(300000, result[0].Price);
        Assert.Equal(1000000, result.Last().Price);
    }

    [Fact]
    public void ApplySorting_PriceDesc()
    {
        var result = _listings.ApplySorting("price", "desc").ToList();
        Assert.Equal(1000000, result[0].Price);
        Assert.Equal(300000, result.Last().Price);
    }

    [Fact]
    public void ApplySorting_DateAsc()
    {
        var result = _listings.ApplySorting("date", "asc").ToList();
        // Oldest listed date first (-5 days)
        Assert.Equal("Side St 3", result[0].Address);
    }

    [Fact]
    public void ApplySorting_DateDesc()
    {
        var result = _listings.ApplySorting("date", "desc").ToList();
        // Newest listed date first (today)
        Assert.Equal("Broadway 2", result[0].Address);
    }

    [Fact]
    public void ApplySorting_LivingAreaAsc()
    {
        var result = _listings.ApplySorting("livingarea", "asc").ToList();
        Assert.Equal(80, result[0].LivingAreaM2);
    }

    [Fact]
    public void ApplySorting_LivingAreaDesc()
    {
        var result = _listings.ApplySorting("livingarea", "desc").ToList();
        Assert.Equal(200, result[0].LivingAreaM2);
    }

    [Fact]
    public void ApplySearchFilter_Postgres_ReturnsQueryable()
    {
        // We can't execute this against InMemory objects as EF.Functions.ILike won't work,
        // but we can ensure the expression is built without error.
        var filter = new ListingFilterDto { SearchTerm = "test" };
        var query = _listings.ApplySearchFilter(filter, isPostgres: true);

        Assert.NotNull(query);
    }

    [Fact]
    public void ApplyCityFilter_Postgres_ReturnsQueryable()
    {
        var query = _listings.ApplyCityFilter("test", isPostgres: true);
        Assert.NotNull(query);
    }
}
