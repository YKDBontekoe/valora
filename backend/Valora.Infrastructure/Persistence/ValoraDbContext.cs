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
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ValoraDbContext).Assembly);
    }
}
