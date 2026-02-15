using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Valora.Domain.Entities;

namespace Valora.Infrastructure.Persistence.Configurations;

public class PriceHistoryConfiguration : IEntityTypeConfiguration<PriceHistory>
{
    public void Configure(EntityTypeBuilder<PriceHistory> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Price).HasColumnType("decimal(18,2)");
        builder.HasOne(e => e.Listing)
              .WithMany(l => l.PriceHistory)
              .HasForeignKey(e => e.ListingId)
              .OnDelete(DeleteBehavior.Cascade);
    }
}
