using Hangfire;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Valora.Infrastructure.Persistence;

namespace Valora.UnitTests.EndpointTests;

public class EndpointTestFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Force Hangfire disable logic in Program.cs
        Environment.SetEnvironmentVariable("Hangfire:Enabled", "false");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "UseInMemoryDatabase", "true" }
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Register NoOp client to satisfy HangfireJobScheduler
            services.AddSingleton<IBackgroundJobClient, NoOpBackgroundJobClient>();
        });
    }

    public override async ValueTask DisposeAsync()
    {
        Environment.SetEnvironmentVariable("Hangfire:Enabled", null);
        await base.DisposeAsync();
    }
}
