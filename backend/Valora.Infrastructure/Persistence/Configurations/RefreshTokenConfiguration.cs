using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Valora.Domain.Entities;

namespace Valora.Infrastructure.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.TokenHash).IsRequired().HasMaxLength(500);
        builder.HasIndex(e => e.TokenHash).IsUnique();
        builder.Ignore(e => e.RawToken);
        builder.HasOne(e => e.User)
              .WithMany(u => u.RefreshTokens)
              .HasForeignKey(e => e.UserId)
              .OnDelete(DeleteBehavior.Cascade);
    }
}
