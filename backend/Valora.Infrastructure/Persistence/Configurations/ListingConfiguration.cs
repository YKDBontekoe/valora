using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Valora.Domain.Entities;
using Valora.Domain.Common;
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
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.IsSoldOrRented);

        // Geospatial Indexes
        builder.HasIndex(e => e.Latitude);
        builder.HasIndex(e => e.Longitude);
        builder.HasIndex(e => new { e.Latitude, e.Longitude });

        // Composite indexes for common filters
        builder.HasIndex(e => new { e.City, e.Price });
        builder.HasIndex(e => new { e.City, e.Bedrooms });
        builder.HasIndex(e => new { e.City, e.LivingAreaM2 });
        builder.HasIndex(e => new { e.City, e.ContextCompositeScore });
        builder.HasIndex(e => new { e.City, e.ContextSafetyScore });
        builder.HasIndex(e => new { e.City, e.LastFundaFetchUtc });
        builder.HasIndex(e => new { e.City, e.LastFundaFetchUtc, e.Price });
        builder.HasIndex(e => e.Address);
        builder.HasIndex(e => e.PropertyType);
        builder.Property(e => e.Address).IsRequired();

        // Constraints
        builder.Property(e => e.FundaId).HasMaxLength(ValidationConstants.Listing.FundaIdMaxLength);
        builder.Property(e => e.Address).HasMaxLength(ValidationConstants.Listing.AddressMaxLength);
        builder.Property(e => e.City).HasMaxLength(ValidationConstants.Listing.CityMaxLength);
        builder.Property(e => e.PostalCode).HasMaxLength(ValidationConstants.Listing.PostalCodeMaxLength);
        builder.Property(e => e.Url).HasMaxLength(ValidationConstants.Listing.UrlMaxLength);
        builder.Property(e => e.ImageUrl).HasMaxLength(ValidationConstants.Listing.ImageUrlMaxLength);
        builder.Property(e => e.PropertyType).HasMaxLength(ValidationConstants.Listing.PropertyTypeMaxLength);
        builder.Property(e => e.Status).HasMaxLength(ValidationConstants.Listing.StatusMaxLength);
        builder.Property(e => e.EnergyLabel).HasMaxLength(ValidationConstants.Listing.EnergyLabelMaxLength);
        builder.Property(e => e.OwnershipType).HasMaxLength(ValidationConstants.Listing.OwnershipTypeMaxLength);
        builder.Property(e => e.CadastralDesignation).HasMaxLength(ValidationConstants.Listing.CadastralDesignationMaxLength);
        builder.Property(e => e.HeatingType).HasMaxLength(ValidationConstants.Listing.HeatingTypeMaxLength);
        builder.Property(e => e.InsulationType).HasMaxLength(ValidationConstants.Listing.InsulationTypeMaxLength);
        builder.Property(e => e.GardenOrientation).HasMaxLength(ValidationConstants.Listing.GardenOrientationMaxLength);
        builder.Property(e => e.ParkingType).HasMaxLength(ValidationConstants.Listing.ParkingTypeMaxLength);
        builder.Property(e => e.AgentName).HasMaxLength(ValidationConstants.Listing.AgentNameMaxLength);
        builder.Property(e => e.RoofType).HasMaxLength(ValidationConstants.Listing.RoofTypeMaxLength);
        builder.Property(e => e.ConstructionPeriod).HasMaxLength(ValidationConstants.Listing.ConstructionPeriodMaxLength);
        builder.Property(e => e.CVBoilerBrand).HasMaxLength(ValidationConstants.Listing.CVBoilerBrandMaxLength);
        builder.Property(e => e.BrokerPhone).HasMaxLength(ValidationConstants.Listing.BrokerPhoneMaxLength);
        builder.Property(e => e.BrokerAssociationCode).HasMaxLength(ValidationConstants.Listing.BrokerAssociationCodeMaxLength);

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
                v => string.IsNullOrWhiteSpace(v) ? null : JsonHelper.Deserialize<ContextReportModel?>(v))
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
