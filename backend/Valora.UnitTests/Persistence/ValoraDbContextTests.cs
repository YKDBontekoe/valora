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
    public void OnModelCreating_AppliesListingConstraints()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ValoraDbContext>()
            .UseInMemoryDatabase(databaseName: "ValoraDbContextTests_Constraints")
            .Options;

        using var context = new ValoraDbContext(options);

        // Act
        // Accessing the Model property triggers OnModelCreating
        var model = context.GetService<IDesignTimeModel>().Model;
        var listingEntity = model.FindEntityType(typeof(Listing));

        // Assert
        Assert.NotNull(listingEntity);

        // Verify MaxLength constraints
        Assert.Equal(50, listingEntity.FindProperty(nameof(Listing.FundaId))?.GetMaxLength());
        Assert.Equal(200, listingEntity.FindProperty(nameof(Listing.Address))?.GetMaxLength());
        Assert.Equal(100, listingEntity.FindProperty(nameof(Listing.City))?.GetMaxLength());
        Assert.Equal(20, listingEntity.FindProperty(nameof(Listing.PostalCode))?.GetMaxLength());
        Assert.Equal(500, listingEntity.FindProperty(nameof(Listing.Url))?.GetMaxLength());
        Assert.Equal(500, listingEntity.FindProperty(nameof(Listing.ImageUrl))?.GetMaxLength());
        Assert.Equal(100, listingEntity.FindProperty(nameof(Listing.PropertyType))?.GetMaxLength());
        Assert.Equal(50, listingEntity.FindProperty(nameof(Listing.Status))?.GetMaxLength());
        Assert.Equal(20, listingEntity.FindProperty(nameof(Listing.EnergyLabel))?.GetMaxLength());
        Assert.Equal(100, listingEntity.FindProperty(nameof(Listing.OwnershipType))?.GetMaxLength());
        Assert.Equal(100, listingEntity.FindProperty(nameof(Listing.CadastralDesignation))?.GetMaxLength());
        Assert.Equal(100, listingEntity.FindProperty(nameof(Listing.HeatingType))?.GetMaxLength());
        Assert.Equal(100, listingEntity.FindProperty(nameof(Listing.InsulationType))?.GetMaxLength());
        Assert.Equal(50, listingEntity.FindProperty(nameof(Listing.GardenOrientation))?.GetMaxLength());
        Assert.Equal(100, listingEntity.FindProperty(nameof(Listing.ParkingType))?.GetMaxLength());
        Assert.Equal(200, listingEntity.FindProperty(nameof(Listing.AgentName))?.GetMaxLength());
        Assert.Equal(100, listingEntity.FindProperty(nameof(Listing.RoofType))?.GetMaxLength());
        Assert.Equal(100, listingEntity.FindProperty(nameof(Listing.ConstructionPeriod))?.GetMaxLength());
        Assert.Equal(100, listingEntity.FindProperty(nameof(Listing.CVBoilerBrand))?.GetMaxLength());
        Assert.Equal(50, listingEntity.FindProperty(nameof(Listing.BrokerPhone))?.GetMaxLength());
        Assert.Equal(20, listingEntity.FindProperty(nameof(Listing.BrokerAssociationCode))?.GetMaxLength());
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

        var workspaceEntity = model.FindEntityType(typeof(Workspace));
        var activityLogEntity = model.FindEntityType(typeof(ActivityLog));
        var savedListingEntity = model.FindEntityType(typeof(SavedListing));
        var listingCommentEntity = model.FindEntityType(typeof(ListingComment));

        // Assert
        Assert.NotNull(workspaceEntity);
        Assert.NotNull(activityLogEntity);
        Assert.NotNull(savedListingEntity);
        Assert.NotNull(listingCommentEntity);

        Assert.Equal(
            DeleteBehavior.NoAction,
            workspaceEntity!.GetForeignKeys().Single(fk => fk.Properties.Single().Name == "OwnerId").DeleteBehavior);

        Assert.Equal(
            DeleteBehavior.NoAction,
            activityLogEntity!.GetForeignKeys().Single(fk => fk.Properties.Single().Name == "ActorId").DeleteBehavior);

        Assert.Equal(
            DeleteBehavior.NoAction,
            savedListingEntity!.GetForeignKeys().Single(fk => fk.Properties.Single().Name == "AddedByUserId").DeleteBehavior);

        Assert.Equal(
            DeleteBehavior.NoAction,
            listingCommentEntity!.GetForeignKeys().Single(fk => fk.Properties.Single().Name == "UserId").DeleteBehavior);

        Assert.Equal(
            DeleteBehavior.NoAction,
            listingCommentEntity.GetForeignKeys().Single(fk => fk.Properties.Single().Name == "ParentCommentId").DeleteBehavior);
    }
}
