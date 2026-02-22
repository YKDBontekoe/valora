using Xunit;

namespace Valora.IntegrationTests;

// These tests verify that the app starts successfully under different Sentry
// configurations. They use IntegrationTestWebAppFactory (InMemory DB) instead
// of the bare WebApplicationFactory<Program> to avoid the EF Core
// "SqlServer + InMemory both registered" error that occurs when no
// DATABASE_URL is set (e.g. in CI).
public class SentryConfigurationTests
{
    private static IntegrationTestWebAppFactory CreateFactory(Dictionary<string, string?> extraConfig)
    {
        // Use a unique DB name per factory so parallel tests don't share state.
        var dbName = $"InMemory:SentryTest-{Guid.NewGuid()}";
        var factory = new IntegrationTestWebAppFactory(dbName);

        // WithWebHostBuilder does not support AddInMemoryCollection layering on
        // IntegrationTestWebAppFactory cleanly; instead we pass values through
        // environment variables, which ConfigureAppConfiguration picks up last (highest priority).
        foreach (var (key, value) in extraConfig)
        {
            if (value is not null)
                Environment.SetEnvironmentVariable(key, value);
        }

        return factory;
    }

    [Fact]
    public void BuildHost_WithSentryProfilingEnabled_DoesNotCrash()
    {
        using var factory = CreateFactory(new Dictionary<string, string?>
        {
            ["SENTRY_DSN"] = "https://d7879133400742199b24471545465c4a@o123456.ingest.sentry.io/123456",
            ["SENTRY_PROFILES_SAMPLE_RATE"] = "1.0",
            ["SENTRY_RELEASE"] = "test-release",
        });

        var client = factory.CreateClient();
        Assert.NotNull(client);
    }

    [Fact]
    public void BuildHost_WithSentryProfilingDisabled_DoesNotCrash()
    {
        using var factory = CreateFactory(new Dictionary<string, string?>
        {
            ["SENTRY_DSN"] = "https://d7879133400742199b24471545465c4a@o123456.ingest.sentry.io/123456",
            ["SENTRY_PROFILES_SAMPLE_RATE"] = "0.0",
        });

        var client = factory.CreateClient();
        Assert.NotNull(client);
    }

    [Fact]
    public void BuildHost_WithoutSentryDsn_DoesNotCrash()
    {
        using var factory = CreateFactory(new Dictionary<string, string?>
        {
            ["SENTRY_DSN"] = "",
        });

        var client = factory.CreateClient();
        Assert.NotNull(client);
    }

    [Fact]
    public void BuildHost_WithSentryFallbacks_DoesNotCrash()
    {
        // No SENTRY_TRACES_SAMPLE_RATE, No SENTRY_PROFILES_SAMPLE_RATE, No SENTRY_RELEASE
        using var factory = CreateFactory(new Dictionary<string, string?>
        {
            ["SENTRY_DSN"] = "https://d7879133400742199b24471545465c4a@o123456.ingest.sentry.io/123456",
        });

        var client = factory.CreateClient();
        Assert.NotNull(client);
    }
}
