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
                services.AddDbContext<ValoraDbContext>(options =>
                    options.UseInMemoryDatabase("ValoraIntegrationTestDb")
                           .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning)));
            }
            else
            {
                services.AddDbContext<ValoraDbContext>(options =>
                    options.UseNpgsql(_connectionString));
            }

            // Remove Hangfire hosted services
            var hostedServices = services.Where(d =>
                d.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService) &&
                d.ImplementationType?.FullName?.Contains("Hangfire") == true)
                .ToList();

            foreach (var descriptor in hostedServices)
            {
                services.Remove(descriptor);
            }
        });
    }
}
