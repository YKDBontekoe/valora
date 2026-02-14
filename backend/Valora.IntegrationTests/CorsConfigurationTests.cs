using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Xunit;

namespace Valora.IntegrationTests;

public class CorsConfigurationTests
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _environmentName;
        private readonly Dictionary<string, string?> _configurationOverride;

        public CustomWebApplicationFactory(string environmentName, Dictionary<string, string?> configurationOverride)
        {
            _environmentName = environmentName;
            _configurationOverride = configurationOverride;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(_environmentName);

            builder.ConfigureAppConfiguration((context, config) =>
            {
                var defaultSettings = new Dictionary<string, string?>
                {
                    { "JWT_SECRET", "TestSecretKeyForIntegrationTestingOnly123!" },
                    { "JWT_ISSUER", "ValoraTest" },
                    { "JWT_AUDIENCE", "ValoraTest" },
                    { "AllowedOrigins:0", null },
                    { "ALLOWED_ORIGINS", null }
                };

                if (_configurationOverride != null)
                {
                    foreach (var kvp in _configurationOverride)
                    {
                        defaultSettings[kvp.Key] = kvp.Value;
                    }
                }

                config.AddInMemoryCollection(defaultSettings);
            });

            builder.ConfigureTestServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ValoraDbContext>));
                if (descriptor != null) services.Remove(descriptor);

                var dbOptions = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions));
                if (dbOptions != null) services.Remove(dbOptions);

                services.AddDbContext<ValoraDbContext>(options =>
                {
                    options.UseInMemoryDatabase("CorsTestDb_" + Guid.NewGuid());
                    options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
                });

                services.AddIdentityCore<ApplicationUser>()
                    .AddRoles<IdentityRole>()
                    .AddEntityFrameworkStores<ValoraDbContext>();
            });
        }
    }

    [Fact]
    public async Task Get_Health_WithAllowedOriginsEnvVar_ShouldAllowOrigin()
    {
        var origin = "http://example.com";
        var config = new Dictionary<string, string?>
        {
            { "ALLOWED_ORIGINS", origin }
        };

        using var factory = new CustomWebApplicationFactory("Production", config);
        var client = factory.CreateClient();

        // Use a non-existent endpoint to avoid DB issues, CORS should still apply if middleware is active
        // Or if /health returns 500, we can still check headers if the exception middleware doesn't strip them.
        // ExceptionHandlingMiddleware is AFTER CORS. So CORS headers *should* be there even on 500.
        // But let's try /health again and ignore 500.
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/health");
        request.Headers.Add("Origin", origin);

        var response = await client.SendAsync(request);

        // We expect CORS headers regardless of status code (mostly)
        Assert.True(response.Headers.Contains("Access-Control-Allow-Origin"),
            $"Expected CORS header. Status: {response.StatusCode}. Headers: {string.Join(", ", response.Headers)}");

        var allowedOrigin = response.Headers.GetValues("Access-Control-Allow-Origin").FirstOrDefault();
        Assert.Equal(origin, allowedOrigin);
    }

    [Fact]
    public async Task Get_Health_Production_WithoutConfig_ShouldAllowAnyOrigin()
    {
        var origin = "http://random-origin.com";
        var config = new Dictionary<string, string?>();

        using var factory = new CustomWebApplicationFactory("Production", config);
        var client = factory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/health");
        request.Headers.Add("Origin", origin);

        var response = await client.SendAsync(request);

        Assert.True(response.Headers.Contains("Access-Control-Allow-Origin"),
            $"Expected CORS header. Status: {response.StatusCode}. Headers: {string.Join(", ", response.Headers)}");

        var allowedOrigin = response.Headers.GetValues("Access-Control-Allow-Origin").FirstOrDefault();
        Assert.Equal("*", allowedOrigin);
    }
}
