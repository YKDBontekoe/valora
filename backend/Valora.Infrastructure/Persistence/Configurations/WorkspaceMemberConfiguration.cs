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
    }
}
