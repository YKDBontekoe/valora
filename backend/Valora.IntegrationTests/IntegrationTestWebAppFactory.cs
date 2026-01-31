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
        // Force Hangfire disabled via environment variable to ensure it's picked up
        // by Program.cs early configuration
        Environment.SetEnvironmentVariable("Hangfire:Enabled", "false");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:DefaultConnection", _connectionString },
                { "Hangfire:Enabled", "false" }
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<ValoraDbContext>));
            services.AddDbContext<ValoraDbContext>(options =>
                options.UseNpgsql(_connectionString));

            // Remove Hangfire hosted services as a safety net
            var hostedServices = services.Where(d =>
                d.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService) &&
                (d.ImplementationType?.FullName?.Contains("Hangfire") == true ||
                 d.ImplementationType?.Name.Contains("BackgroundJobServerHostedService") == true))
                .ToList();

            foreach (var descriptor in hostedServices)
            {
                services.Remove(descriptor);
            }
        });
    }
}