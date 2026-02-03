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
            entity.Property(e => e.Address).IsRequired();
            entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
            // Store Features as JSON - use conversion for broad compatibility (especially InMemory tests)
            entity.Property(e => e.Features)
                .HasColumnType("jsonb") // Hint for Postgres
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new Dictionary<string, string>());
            
            // Phase 4: JSON conversions for list properties
            entity.Property(e => e.ImageUrls)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>());
            
            entity.Property(e => e.FloorPlanUrls)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>());
            
            entity.Property(e => e.OpenHouseDates)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<List<DateTime>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<DateTime>());
            
            // New: Labels from Summary API (e.g., "Nieuw", "Open huis")
            entity.Property(e => e.Labels)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>());
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
