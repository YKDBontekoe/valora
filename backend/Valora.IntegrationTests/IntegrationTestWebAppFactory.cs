using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Valora.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Valora.Domain.Entities;
using Microsoft.AspNetCore.Identity;

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

            // Add DbContext
            if (_connectionString.StartsWith("InMemory"))
            {
                var dbName = _connectionString.Contains(":") ? _connectionString.Split(':')[1] : "ValoraIntegrationTestDb";
                services.AddDbContext<ValoraDbContext>(options =>
                    options.UseInMemoryDatabase(dbName)
                           .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));
            }
            else
            {
                services.AddDbContext<ValoraDbContext>(options =>
                    options.UseNpgsql(_connectionString)
                           .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));
            }

            // Ensure Identity uses the new DbContext
            // When we replaced DbContext, the previous Identity configuration might be stale or broken
            // because AddEntityFrameworkStores<TContext>() registers stores bound to the original TContext configuration.
            // We need to re-register Identity stores to bind them to the new DbContext registration.

            // Note: AddIdentityCore adds the core services (UserManager, etc.).
            // We shouldn't need to call AddIdentityCore again if it was already called in Program.cs,
            // BUT we definitely need to re-bind the EF stores to the new context.

            services.AddIdentityCore<ApplicationUser>()
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<ValoraDbContext>();
        });
    }
}
