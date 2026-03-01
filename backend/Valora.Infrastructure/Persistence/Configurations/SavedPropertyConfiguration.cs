using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Valora.Domain.Entities;

namespace Valora.Infrastructure.Persistence.Configurations;

public class SavedPropertyConfiguration : IEntityTypeConfiguration<SavedProperty>
{
    public void Configure(EntityTypeBuilder<SavedProperty> builder)
    {
        builder.HasKey(e => e.Id);

        builder.HasOne(e => e.Workspace)
            .WithMany(w => w.SavedProperties)
            .HasForeignKey(e => e.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Property)
            .WithMany()
            .HasForeignKey(e => e.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.AddedByUser)
            .WithMany()
            .HasForeignKey(e => e.AddedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.WorkspaceId, e.PropertyId }).IsUnique();
        
        // Add index on WorkspaceId to speed up GetSavedPropertyDtosAsync and GetSavedPropertiesAsync queries
        builder.HasIndex(e => e.WorkspaceId);

        builder.Property(e => e.Notes).HasMaxLength(2000);
    }
}
