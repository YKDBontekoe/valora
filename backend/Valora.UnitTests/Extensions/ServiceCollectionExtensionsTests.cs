using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Valora.Api.Extensions;
using Xunit;

namespace Valora.UnitTests.Extensions;

public class ServiceCollectionExtensionsTests
{
    private readonly Mock<IHostEnvironment> _environment = new();
    private readonly IServiceCollection _services = new ServiceCollection();

    // Helper to get the CorsPolicy from the service provider
    private CorsPolicy? GetDefaultPolicy(IConfiguration config)
    {
        _services.AddCustomCors(config, _environment.Object);
        var provider = _services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<CorsOptions>>().Value;
        return options.GetPolicy(options.DefaultPolicyName!);
    }

    [Fact]
    public void AddCustomCors_WithAllowedOrigins_AllowsSpecificOrigins()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "AllowedOrigins:0", "http://example.com" }
            })
            .Build();

        // Act
        var policy = GetDefaultPolicy(config);

        // Assert
        Assert.NotNull(policy);
        Assert.Contains("http://example.com", policy.Origins);
        Assert.False(policy.AllowAnyOrigin);
    }

    [Fact]
    public void AddCustomCors_WithEnvVar_AllowsSpecificOrigins()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ALLOWED_ORIGINS", "http://env-example.com, https://secure.com" }
            })
            .Build();

        // Act
        var policy = GetDefaultPolicy(config);

        // Assert
        Assert.NotNull(policy);
        Assert.Contains("http://env-example.com", policy.Origins);
        Assert.Contains("https://secure.com", policy.Origins);
    }

    [Fact]
    public void AddCustomCors_InDevelopment_WithoutConfig_AllowsLocalhost()
    {
        // Arrange
        _environment.Setup(e => e.EnvironmentName).Returns("Development");
        var config = new ConfigurationBuilder().Build();

        // Act
        var policy = GetDefaultPolicy(config);

        // Assert
        Assert.NotNull(policy);
        Assert.Contains("http://localhost:3000", policy.Origins);
    }

    [Fact]
    public void AddCustomCors_InProduction_WithoutConfig_BlocksOrigins()
    {
        // Arrange
        _environment.Setup(e => e.EnvironmentName).Returns("Production");
        var config = new ConfigurationBuilder().Build();

        // Act
        var policy = GetDefaultPolicy(config);

        // Assert
        Assert.NotNull(policy);
        // In production fallback, we use SetIsOriginAllowed(origin => false)
        // Origins list might be empty, and IsOriginAllowed should return false
        Assert.Empty(policy.Origins);
        Assert.False(policy.IsOriginAllowed("http://example.com"));
    }
}
