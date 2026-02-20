using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Valora.Domain.Entities;

namespace Valora.Infrastructure.Persistence.Configurations;

public class BatchJobConfiguration : IEntityTypeConfiguration<BatchJob>
{
    public void Configure(EntityTypeBuilder<BatchJob> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Target).IsRequired().HasMaxLength(255);
        builder.Property(x => x.Type).HasConversion<string>();
        builder.Property(x => x.Status).HasConversion<string>();

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_BatchJob_Type", "\"Type\" IN ('CityIngestion')");
            t.HasCheckConstraint("CK_BatchJob_Status", "\"Status\" IN ('Pending', 'Processing', 'Completed', 'Failed')");
        });
    }
}
