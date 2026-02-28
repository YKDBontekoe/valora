using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;
using Xunit;

namespace Valora.UnitTests.Persistence;

public class ValoraDbContextTests
{
    [Fact]
    public void OnModelCreating_AppliesPropertyConstraints()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ValoraDbContext>()
            .UseInMemoryDatabase(databaseName: "ValoraDbContextTests_Constraints")
            .Options;

        using var context = new ValoraDbContext(options);

        // Act
        // Accessing the Model property triggers OnModelCreating
        var model = context.GetService<IDesignTimeModel>().Model;
        var propertyEntity = model.FindEntityType(typeof(Property));

        // Assert
        Assert.NotNull(propertyEntity);

        // Verify MaxLength constraints
        Assert.Equal(50, propertyEntity.FindProperty(nameof(Property.BagId))?.GetMaxLength());
        Assert.Equal(200, propertyEntity.FindProperty(nameof(Property.Address))?.GetMaxLength());
        Assert.Equal(100, propertyEntity.FindProperty(nameof(Property.City))?.GetMaxLength());
        Assert.Equal(20, propertyEntity.FindProperty(nameof(Property.PostalCode))?.GetMaxLength());
    }

    [Fact]
    public void OnModelCreating_DoesNotUsePostgresSpecificColumnTypesOrConstraintSql()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ValoraDbContext>()
            .UseInMemoryDatabase(databaseName: "ValoraDbContextTests_ProviderAgnostic")
            .Options;

        using var context = new ValoraDbContext(options);

        // Act
        var model = context.GetService<IDesignTimeModel>().Model;

        // Assert
        var postgresColumnTypes = model
            .GetEntityTypes()
            .SelectMany(entity => entity.GetProperties())
            .Where(property =>
                string.Equals(
                    property.FindAnnotation(RelationalAnnotationNames.ColumnType)?.Value?.ToString(),
                    "jsonb",
                    StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.Empty(postgresColumnTypes);

        var quotedCheckConstraints = model
            .GetEntityTypes()
            .SelectMany(entity => entity.GetCheckConstraints())
            .Where(constraint => constraint.Sql.Contains('\"'))
            .ToList();

        Assert.Empty(quotedCheckConstraints);
    }

    [Fact]
    public void OnModelCreating_UsesSqlServerSafeDeleteBehaviorsForWorkspaceCollaborationRelationships()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ValoraDbContext>()
            .UseInMemoryDatabase(databaseName: "ValoraDbContextTests_DeleteBehaviors")
            .Options;

        using var context = new ValoraDbContext(options);

        // Act
        var model = context.GetService<IDesignTimeModel>().Model;

        // 1. SavedProperty -> Workspace (Cascade)
        var savedPropertyToWorkspace = model.FindEntityType(typeof(SavedProperty))
            ?.GetForeignKeys()
            .FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(Workspace));
        Assert.Equal(DeleteBehavior.Cascade, savedPropertyToWorkspace?.DeleteBehavior);

        // 2. SavedProperty -> Property (Cascade)
        var savedPropertyToProperty = model.FindEntityType(typeof(SavedProperty))
            ?.GetForeignKeys()
            .FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(Property));
        Assert.Equal(DeleteBehavior.Cascade, savedPropertyToProperty?.DeleteBehavior);

        // 3. PropertyComment -> SavedProperty (Cascade)
        var commentToSavedProperty = model.FindEntityType(typeof(PropertyComment))
            ?.GetForeignKeys()
            .FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(SavedProperty));
        Assert.Equal(DeleteBehavior.Cascade, commentToSavedProperty?.DeleteBehavior);

        // 4. PropertyComment -> User (Restrict - prevent cycle with Workspace/Member/User)
        var commentToUser = model.FindEntityType(typeof(PropertyComment))
            ?.GetForeignKeys()
            .FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(ApplicationUser));
        Assert.Equal(DeleteBehavior.Restrict, commentToUser?.DeleteBehavior);
        
        // 5. PropertyComment -> ParentComment (Restrict - self-referencing)
        var commentToParent = model.FindEntityType(typeof(PropertyComment))
            ?.GetForeignKeys()
            .FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(PropertyComment));
        Assert.Equal(DeleteBehavior.Restrict, commentToParent?.DeleteBehavior);
    }
}
