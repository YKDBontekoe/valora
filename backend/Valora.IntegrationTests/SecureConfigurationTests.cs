using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Valora.IntegrationTests;

[Collection("TestDatabase")]
public class SecureConfigurationTests
{
    private readonly TestDatabaseFixture _fixture;

    public SecureConfigurationTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Cors_Policy_In_Production_Respects_AllowedOrigins()
    {
        // Use a clean factory instance to ensure Production environment
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

        // 1. Request from trusted origin
        var requestTrusted = new HttpRequestMessage(HttpMethod.Get, "/api/health");
        requestTrusted.Headers.Add("Origin", "https://trusted.com");
        var responseTrusted = await client.SendAsync(requestTrusted);

        Assert.True(responseTrusted.Headers.Contains("Access-Control-Allow-Origin"), "Expected CORS header for trusted origin");
        Assert.Equal("https://trusted.com", responseTrusted.Headers.GetValues("Access-Control-Allow-Origin").FirstOrDefault());

        // 2. Request from untrusted origin
        var requestUntrusted = new HttpRequestMessage(HttpMethod.Get, "/api/health");
        requestUntrusted.Headers.Add("Origin", "https://evil.com");
        var responseUntrusted = await client.SendAsync(requestUntrusted);

        Assert.False(responseUntrusted.Headers.Contains("Access-Control-Allow-Origin"), "Did not expect CORS header for untrusted origin");
    }
}
