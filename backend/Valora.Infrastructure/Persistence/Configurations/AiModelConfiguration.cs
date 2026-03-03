using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Valora.Domain.Entities;

namespace Valora.Infrastructure.Persistence.Configurations;

public class AiModelConfiguration : IEntityTypeConfiguration<AiModelConfig>
{
    public void Configure(EntityTypeBuilder<AiModelConfig> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Intent)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(c => c.Intent)
            .IsUnique();

        builder.Property(c => c.PrimaryModel)
            .IsRequired()
            .HasMaxLength(100);

        var stringListComparer = new ValueComparer<List<string>>(
            (c1, c2) => c1 != null && c2 != null ? c1.SequenceEqual(c2) : c1 == c2,
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v != null ? v.GetHashCode() : 0)),
            c => c.ToList());

        builder.Property(c => c.FallbackModels)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>()
            )
            .Metadata.SetValueComparer(stringListComparer);

        builder.Property(c => c.Description)
            .HasMaxLength(500);

        builder.Property(c => c.SafetySettings)
            .HasMaxLength(2000); // Assuming JSON string for safety settings

        // Enforce strict character limits on Intent to match DTO validation
        builder.ToTable(t => t.HasCheckConstraint("CK_AiModelConfig_Intent", "[Intent] NOT LIKE '%[^a-zA-Z0-9_]%'"));
    }
}
