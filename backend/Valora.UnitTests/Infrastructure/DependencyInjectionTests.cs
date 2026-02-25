using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Valora.Application.Common.Interfaces;
using Valora.Application.Common.Models;
using Valora.Application.Enrichment;
using Valora.Infrastructure;
using Valora.Infrastructure.Enrichment;
using Valora.Infrastructure.Services;

namespace Valora.UnitTests.Infrastructure;

public class DependencyInjectionTests
{
    [Fact]
    public void AddInfrastructure_ConfiguresContextOptions_FromEnvironmentVariables()
    {
        var configData = new Dictionary<string, string?>
        {
            { "DATABASE_URL", "Host=localhost;Database=valora" },
            { "CONTEXT_PDOK_BASE_URL", "https://pdok.example" },
            { "CONTEXT_CBS_BASE_URL", "https://cbs.example" },
            { "CONTEXT_OVERPASS_BASE_URL", "https://overpass.example" },
            { "CONTEXT_LUCHTMEETNET_BASE_URL", "https://lucht.example" },
            { "CONTEXT_RESOLVER_CACHE_MINUTES", "10" },
            { "CONTEXT_CBS_CACHE_MINUTES", "20" },
            { "CONTEXT_AMENITIES_CACHE_MINUTES", "30" },
            { "CONTEXT_AIR_CACHE_MINUTES", "40" },
            { "CONTEXT_REPORT_CACHE_MINUTES", "50" }
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var services = new ServiceCollection();
        services.AddInfrastructure(configuration);
        var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<ContextEnrichmentOptions>>().Value;

        Assert.Equal("https://pdok.example", options.PdokBaseUrl);
        Assert.Equal("https://cbs.example", options.CbsBaseUrl);
        Assert.Equal("https://overpass.example", options.OverpassBaseUrl);
        Assert.Equal("https://lucht.example", options.LuchtmeetnetBaseUrl);
        Assert.Equal(10, options.ResolverCacheMinutes);
        Assert.Equal(20, options.CbsCacheMinutes);
        Assert.Equal(30, options.AmenitiesCacheMinutes);
        Assert.Equal(40, options.AirQualityCacheMinutes);
        Assert.Equal(50, options.ReportCacheMinutes);
    }

    [Fact]
    public void AddInfrastructure_RegistersContextServices()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "DATABASE_URL", "Host=localhost;Database=valora" }
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(configuration);

        var provider = services.BuildServiceProvider();

        Assert.IsType<CacheService>(provider.GetRequiredService<ICacheService>());
        Assert.IsType<PdokLocationResolver>(provider.GetRequiredService<ILocationResolver>());
        Assert.IsType<CbsNeighborhoodStatsClient>(provider.GetRequiredService<ICbsNeighborhoodStatsClient>());
        Assert.IsType<OverpassAmenityClient>(provider.GetRequiredService<IAmenityClient>());
        Assert.IsType<LuchtmeetnetAirQualityClient>(provider.GetRequiredService<IAirQualityClient>());
    }

    [Fact]
    public void AddInfrastructure_UsesJwtSecretFromConfiguration_WhenPresent()
    {
        var secret = "my-secret-key-that-is-at-least-32-characters-long";
        var configData = new Dictionary<string, string?>
        {
            { "DATABASE_URL", "Host=localhost;Database=valora" },
            { "JWT_SECRET", secret },
            { "JWT_ISSUER", "valora" },
            { "JWT_AUDIENCE", "valora" }
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var services = new ServiceCollection();
        services.AddInfrastructure(configuration);
        var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<JwtOptions>>().Value;

        Assert.Equal(secret, options.Secret);
        Assert.Equal("valora", options.Issuer);
        Assert.Equal("valora", options.Audience);
    }

    [Fact]
    public void AddInfrastructure_Throws_WhenJwtSecretTooShort()
    {
        var configData = new Dictionary<string, string?>
        {
            { "DATABASE_URL", "Host=localhost;Database=valora" },
            { "JWT_SECRET", "short-secret" },
            { "JWT_ISSUER", "valora" },
            { "JWT_AUDIENCE", "valora" }
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var services = new ServiceCollection();
        services.AddInfrastructure(configuration);
        var provider = services.BuildServiceProvider();

        var ex = Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<JwtOptions>>().Value);

        Assert.Contains("JWT_SECRET must be at least 32 characters long", ex.Message);
    }

    [Fact]
    public void AddInfrastructure_Throws_WhenJwtSecretMissing()
    {
        var configData = new Dictionary<string, string?>
        {
            { "DATABASE_URL", "Host=localhost;Database=valora" },
            { "JWT_ISSUER", "valora" },
            { "JWT_AUDIENCE", "valora" }
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var services = new ServiceCollection();
        services.AddInfrastructure(configuration);
        var provider = services.BuildServiceProvider();

        // When requesting IOptions<JwtOptions>, the configuration delegate runs and should throw
        var ex = Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<JwtOptions>>().Value);

        Assert.Contains("JWT_SECRET is not configured", ex.Message);
    }
}
