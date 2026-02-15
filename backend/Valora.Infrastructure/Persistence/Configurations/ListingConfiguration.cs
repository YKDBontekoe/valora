using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Valora.Domain.Entities;
using Valora.Domain.Models;
using Valora.Infrastructure.Persistence;

namespace Valora.Infrastructure.Persistence.Configurations;

public class ListingConfiguration : IEntityTypeConfiguration<Listing>
{
    public void Configure(EntityTypeBuilder<Listing> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.FundaId).IsUnique();
        builder.HasIndex(e => e.Price);
        builder.HasIndex(e => e.ListedDate);
        builder.HasIndex(e => e.City);
        builder.HasIndex(e => e.PostalCode);
        builder.HasIndex(e => e.Bedrooms);
        builder.HasIndex(e => e.LivingAreaM2);
        builder.HasIndex(e => e.LastFundaFetchUtc);
        builder.HasIndex(e => e.ContextCompositeScore);
        builder.HasIndex(e => e.ContextSafetyScore);

        // Composite indexes for common filters
        builder.HasIndex(e => new { e.City, e.Price });
        builder.HasIndex(e => new { e.City, e.Bedrooms });
        builder.HasIndex(e => new { e.City, e.LivingAreaM2 });
        builder.HasIndex(e => new { e.City, e.ContextCompositeScore });
        builder.HasIndex(e => new { e.City, e.ContextSafetyScore });
        builder.HasIndex(e => new { e.City, e.LastFundaFetchUtc });
        builder.HasIndex(e => e.Address);
        builder.HasIndex(e => e.PropertyType);
        builder.Property(e => e.Address).IsRequired();

        // Constraints
        builder.Property(e => e.FundaId).HasMaxLength(50);
        builder.Property(e => e.Address).HasMaxLength(200);
        builder.Property(e => e.City).HasMaxLength(100);
        builder.Property(e => e.PostalCode).HasMaxLength(20);
        builder.Property(e => e.Url).HasMaxLength(500);
        builder.Property(e => e.ImageUrl).HasMaxLength(500);
        builder.Property(e => e.PropertyType).HasMaxLength(100);
        builder.Property(e => e.Status).HasMaxLength(50);
        builder.Property(e => e.EnergyLabel).HasMaxLength(20);
        builder.Property(e => e.OwnershipType).HasMaxLength(100);
        builder.Property(e => e.CadastralDesignation).HasMaxLength(100);
        builder.Property(e => e.HeatingType).HasMaxLength(100);
        builder.Property(e => e.InsulationType).HasMaxLength(100);
        builder.Property(e => e.GardenOrientation).HasMaxLength(50);
        builder.Property(e => e.ParkingType).HasMaxLength(100);
        builder.Property(e => e.AgentName).HasMaxLength(200);
        builder.Property(e => e.RoofType).HasMaxLength(100);
        builder.Property(e => e.ConstructionPeriod).HasMaxLength(100);
        builder.Property(e => e.CVBoilerBrand).HasMaxLength(100);
        builder.Property(e => e.BrokerPhone).HasMaxLength(50);
        builder.Property(e => e.BrokerAssociationCode).HasMaxLength(20);

        builder.Property(e => e.Price).HasColumnType("decimal(18,2)");

        // Store Features as JSON - use conversion for broad compatibility (especially InMemory tests)
        builder.Property(e => e.Features)
            .HasColumnType("jsonb") // Hint for Postgres
            .HasConversion(
                v => JsonHelper.Serialize(v),
                v => JsonHelper.Deserialize<Dictionary<string, string>>(v))
            .Metadata.SetValueComparer(ValueComparers.DictionaryComparer);

        // Phase 4: JSON conversions for list properties
        builder.Property(e => e.ImageUrls)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonHelper.Serialize(v),
                v => JsonHelper.Deserialize<List<string>>(v))
            .Metadata.SetValueComparer(ValueComparers.StringListComparer);

        builder.Property(e => e.FloorPlanUrls)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonHelper.Serialize(v),
                v => JsonHelper.Deserialize<List<string>>(v))
            .Metadata.SetValueComparer(ValueComparers.StringListComparer);

        builder.Property(e => e.OpenHouseDates)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonHelper.Serialize(v),
                v => JsonHelper.Deserialize<List<DateTime>>(v))
            .Metadata.SetValueComparer(ValueComparers.DateListComparer);

        // New: Labels from Summary API (e.g., "Nieuw", "Open huis")
        builder.Property(e => e.Labels)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonHelper.Serialize(v),
                v => JsonHelper.Deserialize<List<string>>(v))
            .Metadata.SetValueComparer(ValueComparers.StringListComparer);

        builder.Property(e => e.ContextReport)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonHelper.Serialize(v),
                v => JsonHelper.Deserialize<ContextReportModel?>(v))
            .Metadata.SetValueComparer(ValueComparers.ContextReportComparer);

        // Check Constraints
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Listing_ContextCompositeScore", "\"ContextCompositeScore\" >= 0 AND \"ContextCompositeScore\" <= 100");
            t.HasCheckConstraint("CK_Listing_ContextSafetyScore", "\"ContextSafetyScore\" >= 0 AND \"ContextSafetyScore\" <= 100");
            t.HasCheckConstraint("CK_Listing_ContextSocialScore", "\"ContextSocialScore\" >= 0 AND \"ContextSocialScore\" <= 100");
            t.HasCheckConstraint("CK_Listing_ContextAmenitiesScore", "\"ContextAmenitiesScore\" >= 0 AND \"ContextAmenitiesScore\" <= 100");
            t.HasCheckConstraint("CK_Listing_ContextEnvironmentScore", "\"ContextEnvironmentScore\" >= 0 AND \"ContextEnvironmentScore\" <= 100");

            t.HasCheckConstraint("CK_Listing_Price", "\"Price\" > 0");
            t.HasCheckConstraint("CK_Listing_LivingAreaM2", "\"LivingAreaM2\" > 0");
            t.HasCheckConstraint("CK_Listing_Bedrooms", "\"Bedrooms\" >= 0");
        });
    }
}
