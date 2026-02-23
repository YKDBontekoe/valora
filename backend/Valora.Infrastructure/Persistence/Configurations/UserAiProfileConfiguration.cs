using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;
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

        builder.Property(e => e.HouseholdProfile);

        builder.Property(e => e.Preferences);

        // Store List<string> as JSON
        builder.Property(e => e.DisallowedSuggestions)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>()
            );
    }
}
