using Microsoft.EntityFrameworkCore;
using Valora.Infrastructure.Persistence.Extensions;

namespace Valora.UnitTests.Persistence.Extensions;

public class PaginatedListExtensionsTests
{
    private IQueryable<TestEntity> GetQueryable()
    {
        var data = new List<TestEntity>();
        for (int i = 1; i <= 25; i++)
        {
            data.Add(new TestEntity { Id = i, Name = $"Item {i}" });
        }
        return data.AsQueryable();
    }

    [Fact]
    public async Task ToPaginatedListAsync_ShouldReturnCorrectPage()
    {
        // Arrange
        // Note: To test EF Core extensions on IQueryable correctly, we usually need an async provider.
        // Or we can use InMemory database context.
        // Since PaginatedListExtensions uses .CountAsync() and .ToListAsync() which are EF extensions,
        // we can't easily mock IQueryable directly without an async query provider wrapper.
        // Using an InMemory DB context is safer/easier here.

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new TestDbContext(options);
        for (int i = 1; i <= 25; i++)
        {
            context.TestEntities.Add(new TestEntity { Id = i, Name = $"Item {i}" });
        }
        await context.SaveChangesAsync();

        var query = context.TestEntities.AsQueryable();

        // Act
        var result = await query.ToPaginatedListAsync(2, 10);

        // Assert
        Assert.Equal(2, result.PageIndex);
        Assert.Equal(3, result.TotalPages); // 25 items / 10 = 2.5 -> 3
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(10, result.Items.Count);
        Assert.Equal("Item 11", result.Items.First().Name);
        Assert.Equal("Item 20", result.Items.Last().Name);
        Assert.True(result.HasPreviousPage);
        Assert.True(result.HasNextPage);
    }
}

public class TestEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
    public DbSet<TestEntity> TestEntities { get; set; }
}
