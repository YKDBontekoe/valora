using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Valora.Domain.Entities;

namespace Valora.Infrastructure.Persistence;

public class ValoraDbContext : IdentityDbContext<ApplicationUser>
{
    public ValoraDbContext(DbContextOptions<ValoraDbContext> options) : base(options)
    {
    }

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<BatchJob> BatchJobs => Set<BatchJob>();
    public DbSet<Neighborhood> Neighborhoods => Set<Neighborhood>();
    public DbSet<SavedProperty> SavedProperties => Set<SavedProperty>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ValoraDbContext).Assembly);
    }
}
