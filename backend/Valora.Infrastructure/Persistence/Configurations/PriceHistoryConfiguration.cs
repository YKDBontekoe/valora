using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Valora.Domain.Entities;

namespace Valora.Infrastructure.Persistence.Configurations;

public class PriceHistoryConfiguration : IEntityTypeConfiguration<PriceHistory>
{
    public void Configure(EntityTypeBuilder<PriceHistory> builder)
    {
        builder.HasKey(e => e.Id);

        // Optimizes lookups by listing
        builder.HasIndex(e => e.ListingId);
        // Optimizes sorting
        builder.HasIndex(e => e.RecordedAt);

        builder.Property(e => e.Price).HasColumnType("decimal(18,2)");
        builder.HasOne(e => e.Listing)
              .WithMany(l => l.PriceHistory)
              .HasForeignKey(e => e.ListingId)
              .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable(t => t.HasCheckConstraint("CK_PriceHistory_Price", "[Price] > 0"));
    }
}
