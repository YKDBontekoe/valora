using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Valora.Domain.Entities;

namespace Valora.Infrastructure.Persistence;

public class ValoraDbContext : IdentityDbContext<ApplicationUser>
{
    public ValoraDbContext(DbContextOptions<ValoraDbContext> options) : base(options)
    {
    }

    public DbSet<Property> Properties => Set<Property>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<BatchJob> BatchJobs => Set<BatchJob>();
    public DbSet<Neighborhood> Neighborhoods => Set<Neighborhood>();
    public DbSet<AiModelConfig> AiModelConfigs => Set<AiModelConfig>();
    public DbSet<UserAiProfile> UserAiProfiles => Set<UserAiProfile>();

    public DbSet<Workspace> Workspaces => Set<Workspace>();
    public DbSet<WorkspaceMember> WorkspaceMembers => Set<WorkspaceMember>();
    public DbSet<SavedProperty> SavedProperties => Set<SavedProperty>();
    public DbSet<PropertyComment> PropertyComments => Set<PropertyComment>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ValoraDbContext).Assembly);
    }
}
