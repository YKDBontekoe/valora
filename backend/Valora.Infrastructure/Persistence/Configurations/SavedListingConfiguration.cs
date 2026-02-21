using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Valora.Domain.Entities;

namespace Valora.Infrastructure.Persistence.Configurations;

public class SavedListingConfiguration : IEntityTypeConfiguration<SavedListing>
{
    public void Configure(EntityTypeBuilder<SavedListing> builder)
    {
        builder.HasKey(sl => sl.Id);

        builder.HasOne(sl => sl.Listing)
            .WithMany()
            .HasForeignKey(sl => sl.ListingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(sl => sl.AddedByUser)
            .WithMany()
            .HasForeignKey(sl => sl.AddedByUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(sl => sl.Comments)
            .WithOne(c => c.SavedListing)
            .HasForeignKey(c => c.SavedListingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(sl => new { sl.WorkspaceId, sl.ListingId }).IsUnique();
    }
}
