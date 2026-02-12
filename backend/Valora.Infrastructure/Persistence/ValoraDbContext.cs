using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Valora.Domain.Entities;

namespace Valora.Infrastructure.Persistence;

public class ValoraDbContext : IdentityDbContext<ApplicationUser>
{
    public ValoraDbContext(DbContextOptions<ValoraDbContext> options) : base(options)
    {
    }

    public DbSet<Listing> Listings => Set<Listing>();
    public DbSet<PriceHistory> PriceHistories => Set<PriceHistory>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Value Comparers
        // Suppress null warnings with ! because these properties are initialized to empty collections
        // and JsonHelper ensures non-null returns from DB.
        var stringListComparer = new ValueComparer<List<string>>(
            (c1, c2) => c1!.SequenceEqual(c2!),
            c => c!.Aggregate(0, (a, v) => HashCode.Combine(a, v != null ? v.GetHashCode() : 0)),
            c => c!.ToList());

        var dictionaryComparer = new ValueComparer<Dictionary<string, string>>(
            (c1, c2) => c1!.Count == c2!.Count && !c1.Except(c2).Any(),
            // Order by key to ensure GetHashCode is consistent regardless of insertion order
            c => c!.OrderBy(kv => kv.Key).Aggregate(0, (a, v) => HashCode.Combine(a, v.Key.GetHashCode(), v.Value.GetHashCode())),
            c => c!.ToDictionary(entry => entry.Key, entry => entry.Value));

        var dateListComparer = new ValueComparer<List<DateTime>>(
            (c1, c2) => c1!.SequenceEqual(c2!),
            c => c!.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => c!.ToList());

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TokenHash).IsRequired();
            entity.HasIndex(e => e.TokenHash).IsUnique();
            entity.Ignore(e => e.RawToken);
            entity.HasOne(e => e.User)
                  .WithMany(u => u.RefreshTokens)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Listing>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.FundaId).IsUnique();
            entity.HasIndex(e => e.Price);
            entity.HasIndex(e => e.ListedDate);
            entity.HasIndex(e => e.City);
            entity.HasIndex(e => e.PostalCode);
            entity.HasIndex(e => e.Bedrooms);
            entity.HasIndex(e => e.LivingAreaM2);
            entity.HasIndex(e => e.LastFundaFetchUtc);
            entity.HasIndex(e => e.ContextCompositeScore);
            entity.HasIndex(e => e.ContextSafetyScore);

            // Composite indexes for common filters
            entity.HasIndex(e => new { e.City, e.Price });
            entity.HasIndex(e => new { e.City, e.Bedrooms });
            entity.HasIndex(e => new { e.City, e.LivingAreaM2 });
            entity.HasIndex(e => e.Address);
            entity.HasIndex(e => e.PropertyType);
            entity.Property(e => e.Address).IsRequired();

            // Constraints
            entity.Property(e => e.FundaId).HasMaxLength(50);
            entity.Property(e => e.Address).HasMaxLength(200);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.PostalCode).HasMaxLength(20);
            entity.Property(e => e.Url).HasMaxLength(500);
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.PropertyType).HasMaxLength(100);
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.EnergyLabel).HasMaxLength(20);
            entity.Property(e => e.OwnershipType).HasMaxLength(100);
            entity.Property(e => e.CadastralDesignation).HasMaxLength(100);
            entity.Property(e => e.HeatingType).HasMaxLength(100);
            entity.Property(e => e.InsulationType).HasMaxLength(100);
            entity.Property(e => e.GardenOrientation).HasMaxLength(50);
            entity.Property(e => e.ParkingType).HasMaxLength(100);
            entity.Property(e => e.AgentName).HasMaxLength(200);
            entity.Property(e => e.RoofType).HasMaxLength(100);
            entity.Property(e => e.ConstructionPeriod).HasMaxLength(100);
            entity.Property(e => e.CVBoilerBrand).HasMaxLength(100);
            entity.Property(e => e.BrokerPhone).HasMaxLength(50);
            entity.Property(e => e.BrokerAssociationCode).HasMaxLength(20);

            entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
            // Store Features as JSON - use conversion for broad compatibility (especially InMemory tests)
            entity.Property(e => e.Features)
                .HasColumnType("jsonb") // Hint for Postgres
                .HasConversion(
                    v => JsonHelper.Serialize(v),
                    v => JsonHelper.Deserialize<Dictionary<string, string>>(v))
                .Metadata.SetValueComparer(dictionaryComparer);
            
            // Phase 4: JSON conversions for list properties
            entity.Property(e => e.ImageUrls)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonHelper.Serialize(v),
                    v => JsonHelper.Deserialize<List<string>>(v))
                .Metadata.SetValueComparer(stringListComparer);
            
            entity.Property(e => e.FloorPlanUrls)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonHelper.Serialize(v),
                    v => JsonHelper.Deserialize<List<string>>(v))
                .Metadata.SetValueComparer(stringListComparer);
            
            entity.Property(e => e.OpenHouseDates)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonHelper.Serialize(v),
                    v => JsonHelper.Deserialize<List<DateTime>>(v))
                .Metadata.SetValueComparer(dateListComparer);
            
            // New: Labels from Summary API (e.g., "Nieuw", "Open huis")
            entity.Property(e => e.Labels)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonHelper.Serialize(v),
                    v => JsonHelper.Deserialize<List<string>>(v))
                .Metadata.SetValueComparer(stringListComparer);

            // Phase 5: Context Report JSONB
            var contextReportComparer = new ValueComparer<Valora.Domain.Models.ContextReportModel?>(
                (c1, c2) => JsonHelper.Serialize(c1) == JsonHelper.Serialize(c2),
                c => c == null ? 0 : JsonHelper.Serialize(c).GetHashCode(),
                c => c == null ? null : JsonHelper.Deserialize<Valora.Domain.Models.ContextReportModel>(JsonHelper.Serialize(c))!);

            entity.Property(e => e.ContextReport)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonHelper.Serialize(v),
                    v => JsonHelper.Deserialize<Valora.Domain.Models.ContextReportModel?>(v))
                .Metadata.SetValueComparer(contextReportComparer);

            // Check Constraints
            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Listing_ContextCompositeScore", "\"ContextCompositeScore\" >= 0 AND \"ContextCompositeScore\" <= 100");
                t.HasCheckConstraint("CK_Listing_ContextSafetyScore", "\"ContextSafetyScore\" >= 0 AND \"ContextSafetyScore\" <= 100");
                t.HasCheckConstraint("CK_Listing_ContextSocialScore", "\"ContextSocialScore\" >= 0 AND \"ContextSocialScore\" <= 100");
                t.HasCheckConstraint("CK_Listing_ContextAmenitiesScore", "\"ContextAmenitiesScore\" >= 0 AND \"ContextAmenitiesScore\" <= 100");
                t.HasCheckConstraint("CK_Listing_ContextEnvironmentScore", "\"ContextEnvironmentScore\" >= 0 AND \"ContextEnvironmentScore\" <= 100");

                t.HasCheckConstraint("CK_Listing_Price", "\"Price\" > 0");
                t.HasCheckConstraint("CK_Listing_LivingAreaM2", "\"LivingAreaM2\" > 0");
                t.HasCheckConstraint("CK_Listing_Bedrooms", "\"Bedrooms\" >= 0");
            });
        });

        modelBuilder.Entity<PriceHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
            entity.HasOne(e => e.Listing)
                  .WithMany(l => l.PriceHistory)
                  .HasForeignKey(e => e.ListingId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id);
            // Single column indexes on UserId and IsRead are redundant because they are covered by the composite indexes below.
            // CreatedAt might be useful for global date queries, but we primarily query by UserId.
            // entity.HasIndex(e => e.CreatedAt);

            // Composite indexes for efficient sorting and filtering by user
            entity.HasIndex(e => new { e.UserId, e.CreatedAt });
            entity.HasIndex(e => new { e.UserId, e.IsRead, e.CreatedAt });

            entity.Property(e => e.UserId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Body).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.ActionUrl).HasMaxLength(500);
        });

    }
}
