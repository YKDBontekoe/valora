using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Valora.Application.DTOs;
using Valora.Infrastructure.Persistence;
using Xunit;

namespace Valora.IntegrationTests;

public class SupportEndpointTests : BaseIntegrationTest
{
    public SupportEndpointTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task GetSupportStatus_ReturnsDefaultValues_WhenConfigMissing()
    {
        // Act
        var response = await Client.GetAsync("/api/support/status");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var status = await response.Content.ReadFromJsonAsync<SupportStatusDto>();
        Assert.NotNull(status);
        Assert.True(status.IsSupportActive);
        Assert.Equal("Our support team is available 24/7", status.SupportMessage);
    }
}

public class SupportEndpointConfiguredTests : IClassFixture<TestDatabaseFixture>
{
    private readonly WebApplicationFactory<Program> _factory;

    public SupportEndpointConfiguredTests(TestDatabaseFixture fixture)
    {
        _factory = new IntegrationTestWebAppFactory("InMemory:SupportConfigured")
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("Support:IsActive", "false");
                builder.UseSetting("Support:Message", "System Maintenance");
                builder.UseSetting("Support:StatusPageUrl", "https://maintenance.valora.nl");
                builder.UseSetting("Support:ContactEmail", "admin@valora.nl");
            });
    }

    [Fact]
    public async Task GetSupportStatus_ReturnsConfiguredValues()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/support/status");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var status = await response.Content.ReadFromJsonAsync<SupportStatusDto>();
        Assert.NotNull(status);
        Assert.False(status.IsSupportActive);
        Assert.Equal("System Maintenance", status.SupportMessage);
        Assert.Equal("https://maintenance.valora.nl", status.StatusPageUrl);
        Assert.Equal("admin@valora.nl", status.ContactEmail);
    }
}
