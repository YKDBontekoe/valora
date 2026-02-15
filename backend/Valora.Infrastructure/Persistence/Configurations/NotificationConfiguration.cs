using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Valora.Domain.Entities;

namespace Valora.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.IsRead);
        builder.HasIndex(e => e.CreatedAt);

        // Optimizes "Get unread notifications for user X sorted by date"
        builder.HasIndex(e => new { e.UserId, e.IsRead, e.CreatedAt });

        builder.Property(e => e.UserId).IsRequired().HasMaxLength(450);
        builder.Property(e => e.Title).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Body).IsRequired().HasMaxLength(2000);
        builder.Property(e => e.ActionUrl).HasMaxLength(500);
    }
}
