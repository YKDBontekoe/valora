using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Valora.Domain.Entities;

namespace Valora.Infrastructure.Persistence.Configurations;

public class AiModelConfiguration : IEntityTypeConfiguration<AiModelConfig>
{
    public void Configure(EntityTypeBuilder<AiModelConfig> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Feature)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(c => c.Feature)
            .IsUnique();

        builder.Property(c => c.ModelId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Description)
            .HasMaxLength(500);

        builder.Property(c => c.SafetySettings)
            .HasMaxLength(2000); // Assuming JSON string for safety settings

        builder.Property(c => c.SystemPrompt)
            .HasMaxLength(4000);

        // Enforce strict character limits on Feature to match DTO validation
        builder.ToTable(t => t.HasCheckConstraint("CK_AiModelConfig_Feature", "[Feature] NOT LIKE '%[^a-zA-Z0-9_]%'"));
    }
}
