using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Valora.Domain.Entities;
using Valora.Domain.Common;
using Valora.Domain.Models;
using Valora.Infrastructure.Persistence;

namespace Valora.Infrastructure.Persistence.Configurations;

public class PropertyConfiguration : IEntityTypeConfiguration<Property>
{
    public void Configure(EntityTypeBuilder<Property> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.BagId).IsUnique();
        builder.HasIndex(e => e.City);
        builder.HasIndex(e => e.PostalCode);
        builder.HasIndex(e => e.ContextCompositeScore);
        builder.HasIndex(e => e.ContextSafetyScore);

        // Geospatial Indexes
        builder.HasIndex(e => e.Latitude);
        builder.HasIndex(e => e.Longitude);
        builder.HasIndex(e => new { e.Latitude, e.Longitude });

        builder.Property(e => e.Address).IsRequired().HasMaxLength(ValidationConstants.Listing.AddressMaxLength);
        builder.Property(e => e.BagId).HasMaxLength(50);
        builder.Property(e => e.City).HasMaxLength(ValidationConstants.Listing.CityMaxLength);
        builder.Property(e => e.PostalCode).HasMaxLength(ValidationConstants.Listing.PostalCodeMaxLength);

        builder.Property(e => e.ContextReport)
            .HasConversion(
                v => JsonHelper.Serialize(v),
                v => string.IsNullOrWhiteSpace(v) ? null : JsonHelper.Deserialize<ContextReportModel?>(v))
            .Metadata.SetValueComparer(ValueComparers.ContextReportComparer);

        // Check Constraints
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Property_ContextCompositeScore", "[ContextCompositeScore] >= 0 AND [ContextCompositeScore] <= 100");
            t.HasCheckConstraint("CK_Property_ContextSafetyScore", "[ContextSafetyScore] >= 0 AND [ContextSafetyScore] <= 100");
            t.HasCheckConstraint("CK_Property_ContextSocialScore", "[ContextSocialScore] >= 0 AND [ContextSocialScore] <= 100");
            t.HasCheckConstraint("CK_Property_ContextAmenitiesScore", "[ContextAmenitiesScore] >= 0 AND [ContextAmenitiesScore] <= 100");
            t.HasCheckConstraint("CK_Property_ContextEnvironmentScore", "[ContextEnvironmentScore] >= 0 AND [ContextEnvironmentScore] <= 100");
        });
    }
}
