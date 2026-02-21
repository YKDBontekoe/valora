using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
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

        builder.Property(c => c.FallbackModels)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>()
            );

        builder.Property(c => c.Description)
            .HasMaxLength(500);

        builder.Property(c => c.SafetySettings)
            .HasMaxLength(2000); // Assuming JSON string for safety settings
    }
}
