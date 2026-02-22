using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Shouldly;
using Valora.Application.DTOs;
using Xunit;

namespace Valora.IntegrationTests;

[Collection("TestDatabase")]
public class RateLimitingTests : BaseIntegrationTest
{
    public RateLimitingTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task Admin_IsNot_RateLimited_WhenExceedingStrictLimit()
    {
        await AuthenticateAsAdminAsync();
        var token = Client.DefaultRequestHeaders.Authorization?.Parameter;
        token.ShouldNotBeNull();

        using var client = Factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "RateLimiting:StrictLimit", "5" }
                });
            });
        }).CreateClient();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        for (int i = 0; i < 10; i++)
        {
            var response = await client.GetAsync("/api/admin/users?page=1&pageSize=10");
            response.StatusCode.ShouldNotBe(HttpStatusCode.TooManyRequests);
            response.EnsureSuccessStatusCode();
        }
    }

    [Fact]
    public async Task User_Is_RateLimited_WhenExceedingStrictLimit()
    {
        await AuthenticateAsync();
        var token = Client.DefaultRequestHeaders.Authorization?.Parameter;
        token.ShouldNotBeNull();

        using var client = Factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "RateLimiting:StrictLimit", "5" }
                });
            });
        }).CreateClient();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // 1. Hit an endpoint with "strict" policy (/api/admin/users)
        // User is not Admin, so will get 403 Forbidden eventually, but counted towards rate limit.

        for (int i = 0; i < 5; i++)
        {
            var response = await client.GetAsync("/api/admin/users?page=1&pageSize=10");
            // Should be 403 Forbidden because User != Admin
            response.StatusCode.ShouldBe(HttpStatusCode.Forbidden, $"Request {i + 1} should be Forbidden");
        }

        // 2. The 6th request should be rate limited (429)
        var blockedResponse = await client.GetAsync("/api/admin/users?page=1&pageSize=10");
        blockedResponse.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task RateLimit_ShouldNotCrash_WithInvalidConfiguration()
    {
        // Arrange
        using var client = Factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "RateLimiting:FixedLimit", "0" }, // Invalid
                    { "RateLimiting:StrictLimit", "-10" } // Invalid
                });
            });
        }).CreateClient();

        // Act & Assert
        // Application should fallback to default limits and work
        var response = await client.GetAsync("/api/health");

        // Assert
        response.EnsureSuccessStatusCode();
    }
}
