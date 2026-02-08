using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Valora.IntegrationTests;

[Collection("TestDatabase")]
public class CorsTests
{
    private readonly TestDatabaseFixture _fixture;

    public CorsTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Cors_Blocks_UnknownOrigin_When_AllowedOrigins_Configured()
    {
        // Simulate Production environment with specific Allowed Origins
        var factory = _fixture.Factory!.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "ALLOWED_ORIGINS", "https://trusted.com" }
                });
            });
        });

        var client = factory.CreateClient();

        // Send a request with an Origin header
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/health");
        request.Headers.Add("Origin", "https://evil.com");
        request.Headers.Add("Access-Control-Request-Method", "GET");

        var response = await client.SendAsync(request);

        // Assert
        // Standard CORS behavior: If blocked, the Access-Control-Allow-Origin header is missing.
        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"), "Should not allow unknown origin.");
    }

    [Fact]
    public async Task Cors_Allows_KnownOrigin_When_AllowedOrigins_Configured()
    {
        // Simulate Production environment with specific Allowed Origins
        var factory = _fixture.Factory!.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "ALLOWED_ORIGINS", "https://trusted.com;https://also-trusted.com" }
                });
            });
        });

        var client = factory.CreateClient();

        // Send a request with a trusted Origin header
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/health");
        request.Headers.Add("Origin", "https://trusted.com");
        request.Headers.Add("Access-Control-Request-Method", "GET");

        var response = await client.SendAsync(request);

        // Assert
        Assert.True(response.Headers.Contains("Access-Control-Allow-Origin"), "Should allow known origin.");
        var allowedOrigin = response.Headers.GetValues("Access-Control-Allow-Origin").FirstOrDefault();
        Assert.Equal("https://trusted.com", allowedOrigin);
    }

    [Fact]
    public async Task Cors_Blocks_All_When_AllowedOrigins_Missing_In_Production()
    {
        // Simulate Production environment with NO Allowed Origins
        var factory = _fixture.Factory!.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.ConfigureAppConfiguration((context, config) =>
            {
                // Clear existing or ensure empty
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "ALLOWED_ORIGINS", "" }
                });
            });
        });

        var client = factory.CreateClient();

        // Send a request with any Origin
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/health");
        request.Headers.Add("Origin", "https://any-site.com");
        request.Headers.Add("Access-Control-Request-Method", "GET");

        var response = await client.SendAsync(request);

        // Assert
        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"), "Should block all origins by default in Production.");
    }

    [Fact]
    public async Task Cors_Allows_Any_In_Development()
    {
        // Simulate Development environment with NO Allowed Origins
        var factory = _fixture.Factory!.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "ALLOWED_ORIGINS", "" }
                });
            });
        });

        var client = factory.CreateClient();

        // Send a request with any Origin
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/health");
        request.Headers.Add("Origin", "http://localhost:3000");
        request.Headers.Add("Access-Control-Request-Method", "GET");

        var response = await client.SendAsync(request);

        // Assert
        Assert.True(response.Headers.Contains("Access-Control-Allow-Origin"), "Should allow any origin in Development.");
        var allowedOrigin = response.Headers.GetValues("Access-Control-Allow-Origin").FirstOrDefault();
        // AllowAnyOrigin typically returns '*' or echoes the origin depending on credentials setting.
        // If AllowCredentials is meant to be false (default), it echoes origin often with AllowAnyOrigin() + AllowAnyHeader/Method
        // Wait, AllowAnyOrigin() sends '*'.
        Assert.Equal("*", allowedOrigin);
    }
}
