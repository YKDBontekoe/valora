using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Valora.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Hangfire;

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
                { "ConnectionStrings:DefaultConnection", _connectionString }
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<ValoraDbContext>));
            services.AddDbContext<ValoraDbContext>(options =>
                options.UseNpgsql(_connectionString));

            // Register NoOp client to satisfy HangfireJobScheduler dependency
            // even if Hangfire itself isn't fully configured/enabled in tests.
            services.AddSingleton<IBackgroundJobClient, NoOpBackgroundJobClient>();
        });
    }
}