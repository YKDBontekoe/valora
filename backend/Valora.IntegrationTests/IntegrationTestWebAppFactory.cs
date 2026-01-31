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
                { "ConnectionStrings:DefaultConnection", _connectionString },
                { "Hangfire:Enabled", "false" }
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<ValoraDbContext>));
            services.AddDbContext<ValoraDbContext>(options =>
                options.UseNpgsql(_connectionString));

            // Remove Hangfire hosted services to prevent them from starting
            // This is necessary because Program.cs might register them before
            // the configuration override is picked up.
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