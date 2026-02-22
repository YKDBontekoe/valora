using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Valora.Domain.Entities;

namespace Valora.Infrastructure.Persistence.Configurations;

public class BatchJobConfiguration : IEntityTypeConfiguration<BatchJob>
{
    public void Configure(EntityTypeBuilder<BatchJob> builder)
    {
        builder.HasKey(x => x.Id);

        // Optimizes polling for pending jobs
        builder.HasIndex(e => e.Status);
        // Optimizes sorting by creation date
        builder.HasIndex(e => e.CreatedAt);

        builder.Property(x => x.Target).IsRequired().HasMaxLength(255);
        builder.Property(x => x.Type).HasConversion<string>();
        builder.Property(x => x.Status).HasConversion<string>();

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_BatchJob_Type", "\"Type\" IN ('CityIngestion', 'MapGeneration')");
            t.HasCheckConstraint("CK_BatchJob_Status", "\"Status\" IN ('Pending', 'Processing', 'Completed', 'Failed')");
        });
    }
}
