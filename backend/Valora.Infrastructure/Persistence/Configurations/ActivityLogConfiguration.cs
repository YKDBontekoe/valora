using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Valora.Domain.Entities;

namespace Valora.Infrastructure.Persistence.Configurations;

public class ActivityLogConfiguration : IEntityTypeConfiguration<ActivityLog>
{
    public void Configure(EntityTypeBuilder<ActivityLog> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Type).HasConversion<string>();
        builder.Property(a => a.Summary).IsRequired().HasMaxLength(500);
        builder.Property(a => a.Metadata);

        builder.HasOne(a => a.Actor)
            .WithMany()
            .HasForeignKey(a => a.ActorId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(a => a.Workspace)
            .WithMany(w => w.ActivityLogs)
            .HasForeignKey(a => a.WorkspaceId)
            .OnDelete(DeleteBehavior.SetNull);

        // Optimize fetching recent activity logs for a workspace
        builder.HasIndex(a => new { a.WorkspaceId, a.CreatedAt });
    }
}
