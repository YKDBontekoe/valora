using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using Xunit;

namespace Valora.IntegrationTests;

public class SentryConfigurationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public SentryConfigurationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public void BuildHost_WithSentryProfilingEnabled_DoesNotCrash()
    {
        // Arrange
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "SENTRY_DSN", "https://d7879133400742199b24471545465c4a@o123456.ingest.sentry.io/123456" },
                    { "SENTRY_PROFILES_SAMPLE_RATE", "1.0" },
                    { "SENTRY_RELEASE", "test-release" }
                });
            });
        });

        // Act
        var client = factory.CreateClient();

        // Assert
        Assert.NotNull(client);
    }

    [Fact]
    public void BuildHost_WithSentryProfilingDisabled_DoesNotCrash()
    {
        // Arrange
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "SENTRY_DSN", "https://d7879133400742199b24471545465c4a@o123456.ingest.sentry.io/123456" },
                    { "SENTRY_PROFILES_SAMPLE_RATE", "0.0" }
                });
            });
        });

        // Act
        var client = factory.CreateClient();

        // Assert
        Assert.NotNull(client);
    }

    [Fact]
    public void BuildHost_WithoutSentryDsn_DoesNotCrash()
    {
        // Arrange
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "SENTRY_DSN", "" }
                });
            });
        });

        // Act
        var client = factory.CreateClient();

        // Assert
        Assert.NotNull(client);
    }

    [Fact]
    public void BuildHost_WithSentryFallbacks_DoesNotCrash()
    {
        // Arrange
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "SENTRY_DSN", "https://d7879133400742199b24471545465c4a@o123456.ingest.sentry.io/123456" }
                    // No SENTRY_TRACES_SAMPLE_RATE, No SENTRY_PROFILES_SAMPLE_RATE, No SENTRY_RELEASE
                });
            });
        });

        // Act
        var client = factory.CreateClient();

        // Assert
        Assert.NotNull(client);
    }
}
