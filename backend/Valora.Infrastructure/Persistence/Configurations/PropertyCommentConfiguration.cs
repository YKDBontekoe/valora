using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;

namespace Valora.Infrastructure.Persistence.Configurations;

public class PropertyCommentConfiguration : IEntityTypeConfiguration<PropertyComment>
{
    public void Configure(EntityTypeBuilder<PropertyComment> builder)
    {
        builder.HasKey(e => e.Id);

        builder.HasOne(e => e.SavedProperty)
            .WithMany(sp => sp.Comments)
            .HasForeignKey(e => e.SavedPropertyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ParentComment)
            .WithMany(c => c.Replies)
            .HasForeignKey(e => e.ParentCommentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.Content).IsRequired().HasMaxLength(2000);

        builder.Property(e => e.Reactions)
            .HasConversion(
                v => JsonHelper.Serialize(v),
                v => JsonHelper.Deserialize<Dictionary<string, List<string>>>(v))
            .Metadata.SetValueComparer(ValueComparers.DictionaryListComparer);
    }
}
