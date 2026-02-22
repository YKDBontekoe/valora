using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;

namespace Valora.Infrastructure.Persistence.Configurations;

public class ListingCommentConfiguration : IEntityTypeConfiguration<ListingComment>
{
    public void Configure(EntityTypeBuilder<ListingComment> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Content).IsRequired().HasMaxLength(2000);

        builder.HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.ParentComment)
            .WithMany(p => p.Replies)
            .HasForeignKey(c => c.ParentCommentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(c => c.Reactions)
            .HasConversion(
                v => JsonHelper.Serialize(v),
                v => JsonHelper.Deserialize<Dictionary<string, List<string>>>(v))
            .Metadata.SetValueComparer(
                new ValueComparer<Dictionary<string, List<string>>>(
                    (c1, c2) => JsonHelper.Serialize(c1) == JsonHelper.Serialize(c2),
                    c => c == null ? 0 : JsonHelper.Serialize(c).GetHashCode(),
                    c => JsonHelper.Deserialize<Dictionary<string, List<string>>>(JsonHelper.Serialize(c ?? new Dictionary<string, List<string>>()))
                ));
    }
}
