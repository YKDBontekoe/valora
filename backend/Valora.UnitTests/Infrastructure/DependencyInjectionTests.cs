using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Valora.Application.Scraping;
using Valora.Infrastructure;

namespace Valora.UnitTests.Infrastructure;

public class DependencyInjectionTests
{
    [Fact]
    public void AddInfrastructure_ConfiguresScraperOptions_FromEnvironmentVariables()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            { "DATABASE_URL", "Host=localhost;Database=valora" },
            { "SCRAPER_SEARCH_URLS", "http://url1.com;http://url2.com" },
            { "SCRAPER_DELAY_MS", "5000" },
            { "SCRAPER_MAX_RETRIES", "10" },
            { "SCRAPER_CRON", "0 0 * * *" }
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var services = new ServiceCollection();

        // Act
        services.AddInfrastructure(configuration);
        var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetRequiredService<IOptions<ScraperOptions>>().Value;

        Assert.Equal(2, options.SearchUrls.Count);
        Assert.Contains("http://url1.com", options.SearchUrls);
        Assert.Contains("http://url2.com", options.SearchUrls);
        Assert.Equal(5000, options.DelayBetweenRequestsMs);
        Assert.Equal(10, options.MaxRetries);
        Assert.Equal("0 0 * * *", options.CronExpression);
    }

    [Fact]
    public void AddInfrastructure_ConfiguresScraperOptions_WithDefaults_WhenEnvVarsMissing()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            { "DATABASE_URL", "Host=localhost;Database=valora" }
            // Missing Scraper vars
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var services = new ServiceCollection();

        // Act
        services.AddInfrastructure(configuration);
        var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetRequiredService<IOptions<ScraperOptions>>().Value;

        // Verify defaults (from ScraperOptions class definition)
        Assert.Equal(2000, options.DelayBetweenRequestsMs);
        Assert.Equal(3, options.MaxRetries);
        Assert.Equal("0 */6 * * *", options.CronExpression);
        Assert.Empty(options.SearchUrls);
    }
}
