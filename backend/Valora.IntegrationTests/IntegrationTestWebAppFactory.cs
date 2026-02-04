using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Valora.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;

namespace Valora.IntegrationTests;

public class IntegrationTestWebAppFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    public IntegrationTestWebAppFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "DATABASE_URL", _connectionString },
                { "HANGFIRE_ENABLED", "false" },
                { "JWT_SECRET", "TestSecretKeyForIntegrationTestingOnly123!" },
                { "JWT_ISSUER", "ValoraTest" },
                { "JWT_AUDIENCE", "ValoraTest" },
                { "JWT_EXPIRY_MINUTES", "15" }
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Remove existing DbContext registrations
            services.RemoveAll<DbContextOptions<ValoraDbContext>>();
            services.RemoveAll<DbContextOptions>();
            services.RemoveAll<ValoraDbContext>();

            // Remove implicit configuration that might hold the old delegate
            // This is likely why Npgsql was still sticking around!
            var configType = Type.GetType("Microsoft.EntityFrameworkCore.Infrastructure.IDbContextOptionsConfiguration`1, Microsoft.EntityFrameworkCore");
            if (configType != null)
            {
                 var genericConfigType = configType.MakeGenericType(typeof(ValoraDbContext));
                 services.RemoveAll(genericConfigType);
            }

            // Also try to find it via interface matching if reflection fails or assembly issues
            var configServices = services.Where(d => d.ServiceType.Name.Contains("IDbContextOptionsConfiguration") &&
                                                     d.ServiceType.IsGenericType &&
                                                     d.ServiceType.GetGenericArguments()[0] == typeof(ValoraDbContext)).ToList();
            foreach (var s in configServices) services.Remove(s);


            // Nuclear cleanup of Npgsql if present
            var npgsqlServices = services.Where(d =>
                d.ServiceType.FullName?.Contains("Npgsql") == true ||
                d.ImplementationType?.FullName?.Contains("Npgsql") == true).ToList();

            foreach (var s in npgsqlServices)
            {
                services.Remove(s);
            }

            if (_connectionString == "InMemory")
            {
                // Use a unique database name per factory instance to prevent test interference
                var dbName = Guid.NewGuid().ToString();
                services.AddDbContext<ValoraDbContext>(options =>
                    options.UseInMemoryDatabase(dbName));
            }
            else
            {
                services.AddDbContext<ValoraDbContext>(options =>
                    options.UseNpgsql(_connectionString));
            }

            // Remove Hangfire hosted services ONLY if explicitly disabled
            // We can't easily check final config here, but we can check if we are in InMemory mode
            // If InMemory, and using MemoryStorage (which we enabled in Program.cs), it is safe to keep them.
            // However, ScraperEndpointsTests enables Hangfire.
            // If we remove them, UseHangfireDashboard crashes.
            // So we should NOT remove them if they are using MemoryStorage (which doesn't need external infra).

            // To be safe, let's only remove them if NOT using InMemory/MemoryStorage logic.
            // But Program.cs logic relies on HANGFIRE_ENABLED.
            // If HANGFIRE_ENABLED is false, Program.cs doesn't add them. So nothing to remove.
            // If HANGFIRE_ENABLED is true, Program.cs adds them.
            // If we remove them, it crashes.
            // So we should NEVER remove them here if Program.cs logic is correct.
            // The only reason to remove them is if they start side effects we don't want.
            // With MemoryStorage, side effects are minimal (in-memory job server).
            // So let's remove this block.
        });
    }
}
