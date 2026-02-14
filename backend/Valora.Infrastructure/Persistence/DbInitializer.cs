using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Valora.Application.Common.Interfaces;

namespace Valora.Infrastructure.Persistence;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, IConfiguration configuration, IHostEnvironment environment)
    {
        if (environment.IsEnvironment("Testing"))
        {
            return;
        }

        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ValoraDbContext>>();

        try
        {
            if (dbContext.Database.IsRelational())
            {
                dbContext.Database.Migrate();
            }

            // Seed Admin User
            var adminEmail = configuration["ADMIN_EMAIL"];
            var adminPassword = configuration["ADMIN_PASSWORD"];

            if (!string.IsNullOrEmpty(adminEmail) && !string.IsNullOrEmpty(adminPassword))
            {
                var identityService = scope.ServiceProvider.GetRequiredService<IIdentityService>();

                await identityService.EnsureRoleAsync("Admin");

                var user = await identityService.GetUserByEmailAsync(adminEmail);
                if (user == null)
                {
                    // Only create and promote if user does NOT exist (prevents takeover of existing accounts)
                    var (createResult, userId) = await identityService.CreateUserAsync(adminEmail, adminPassword);
                    if (createResult.Succeeded)
                    {
                        var roleResult = await identityService.AddToRoleAsync(userId, "Admin");
                        if (roleResult.Succeeded)
                        {
                            logger.LogInformation("Successfully seeded initial Admin user.");
                        }
                        else
                        {
                            logger.LogWarning("Created Admin user but failed to assign role. Check identity logs.");
                        }
                    }
                    else
                    {
                        logger.LogWarning("Failed to create initial Admin user. Check identity logs.");
                    }
                }
                else
                {
                    logger.LogWarning("Admin seeding: User configured in ADMIN_EMAIL already exists. Skipping automatic promotion to prevent privilege escalation.");
                }
            }
        }
        catch (Exception ex)
        {
            // Log error or handle it (e.g., if database is not ready yet)
            logger.LogError(ex, "An error occurred while migrating the database.");
        }
    }
}
