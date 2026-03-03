using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Valora.Domain.Entities;

namespace Valora.Infrastructure.Persistence.Configurations;

public class UserAiProfileConfiguration : IEntityTypeConfiguration<UserAiProfile>
{
    public void Configure(EntityTypeBuilder<UserAiProfile> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.UserId)
            .IsRequired()
            .HasMaxLength(450); // Standard Identity User ID length

        builder.HasIndex(e => e.UserId).IsUnique();

        builder.Property(e => e.HouseholdProfile)
            .HasMaxLength(4000);

        builder.Property(e => e.Preferences)
            .HasMaxLength(4000);

        var stringListComparer = new ValueComparer<List<string>>(
            (c1, c2) => c1 != null && c2 != null ? c1.SequenceEqual(c2) : c1 == c2,
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v != null ? v.GetHashCode() : 0)),
            c => c.ToList());

        builder.Property(e => e.DisallowedSuggestions)
            .HasMaxLength(4000)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>()
            )
            .Metadata.SetValueComparer(stringListComparer);
    }
}
