using Microsoft.EntityFrameworkCore;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;
using Xunit;

namespace Valora.UnitTests.Persistence;

public class ValoraDbContextTests
{
    private readonly DbContextOptions<ValoraDbContext> _options;

    public ValoraDbContextTests()
    {
        _options = new DbContextOptionsBuilder<ValoraDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public void OnModelCreating_ShouldDefineCompositeIndexes_ForNotification()
    {
        // Arrange
        using var context = new ValoraDbContext(_options);

        // Act
        // Accessing Model triggers OnModelCreating
        var entity = context.Model.FindEntityType(typeof(Notification));

        // Assert
        Assert.NotNull(entity);

        var indexes = entity.GetIndexes().ToList();

        // Check for composite index (UserId, CreatedAt)
        // Note: EF Core stores properties in the order they were defined in the composite key
        var userDateIndex = indexes.FirstOrDefault(i =>
            i.Properties.Count == 2 &&
            i.Properties[0].Name == nameof(Notification.UserId) &&
            i.Properties[1].Name == nameof(Notification.CreatedAt));

        Assert.NotNull(userDateIndex);

        // Check for composite index (UserId, IsRead, CreatedAt)
        var userReadDateIndex = indexes.FirstOrDefault(i =>
            i.Properties.Count == 3 &&
            i.Properties[0].Name == nameof(Notification.UserId) &&
            i.Properties[1].Name == nameof(Notification.IsRead) &&
            i.Properties[2].Name == nameof(Notification.CreatedAt));

        Assert.NotNull(userReadDateIndex);
    }
}
