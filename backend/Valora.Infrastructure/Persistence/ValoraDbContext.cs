using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

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

            // Composite indexes for common filters
            entity.HasIndex(e => new { e.City, e.Price });
            entity.HasIndex(e => new { e.City, e.Bedrooms });
            entity.HasIndex(e => new { e.City, e.LivingAreaM2 });
            entity.Property(e => e.Address).IsRequired();
            entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
            // Store Features as JSON - use conversion for broad compatibility (especially InMemory tests)
            entity.Property(e => e.Features)
                .HasColumnType("jsonb") // Hint for Postgres
                .HasConversion(
                    v => JsonHelper.Serialize(v),
                    v => JsonHelper.Deserialize<Dictionary<string, string>>(v));
            
            // Phase 4: JSON conversions for list properties
            entity.Property(e => e.ImageUrls)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonHelper.Serialize(v),
                    v => JsonHelper.Deserialize<List<string>>(v));
            
            entity.Property(e => e.FloorPlanUrls)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonHelper.Serialize(v),
                    v => JsonHelper.Deserialize<List<string>>(v));
            
            entity.Property(e => e.OpenHouseDates)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonHelper.Serialize(v),
                    v => JsonHelper.Deserialize<List<DateTime>>(v));
            
            // New: Labels from Summary API (e.g., "Nieuw", "Open huis")
            entity.Property(e => e.Labels)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonHelper.Serialize(v),
                    v => JsonHelper.Deserialize<List<string>>(v));
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
    }
}
