using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Valora.Domain.Entities;

namespace Valora.Infrastructure.Persistence.Configurations;

public class WorkspaceMemberConfiguration : IEntityTypeConfiguration<WorkspaceMember>
{
    public void Configure(EntityTypeBuilder<WorkspaceMember> builder)
    {
        builder.HasKey(wm => wm.Id);

        builder.HasOne(wm => wm.User)
            .WithMany()
            .HasForeignKey(wm => wm.UserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(wm => wm.Role)
            .HasConversion<string>();

        builder.HasIndex(wm => new { wm.WorkspaceId, wm.UserId })
            .IsUnique()
            .HasFilter("\"UserId\" IS NOT NULL");

        builder.HasIndex(wm => new { wm.WorkspaceId, wm.InvitedEmail })
            .IsUnique()
            .HasFilter("\"InvitedEmail\" IS NOT NULL");

        // Optimize finding workspaces for a user (used in GetUserWorkspacesAsync)
        // Since the main query filters by UserId, an index on UserId is crucial.
        // It's a FK so usually indexed by default, but we can make it explicit/compound if needed.
        // The query is: .Where(w => w.Members.Any(m => m.UserId == userId))
        // So we need to quickly find Member records by UserId.
        builder.HasIndex(wm => wm.UserId);
    }
}
