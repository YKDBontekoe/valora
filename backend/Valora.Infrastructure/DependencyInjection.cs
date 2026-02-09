using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Valora.Application.Common.Interfaces;
using Valora.Application.Common.Models;
using Valora.Application.Enrichment;
using Valora.Application.Services;
using Valora.Infrastructure.Enrichment;
using Valora.Infrastructure.Enrichment.Builders;
using Valora.Infrastructure.Persistence;
using Valora.Infrastructure.Persistence.Repositories;
using Valora.Infrastructure.Services;

namespace Valora.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var rawConnectionString = configuration["DATABASE_URL"] ?? configuration.GetConnectionString("DefaultConnection");
        var connectionString = ConnectionStringParser.BuildConnectionString(rawConnectionString);

        services.AddDbContext<ValoraDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorCodesToAdd: null)));

        // Repositories
        services.AddScoped<IListingRepository, ListingRepository>();
        services.AddScoped<IPriceHistoryRepository, PriceHistoryRepository>();

        // Services
        services.AddSingleton(TimeProvider.System);
        services.AddMemoryCache();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IAiService, OpenRouterAiService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IContextReportService, ContextReportService>();
        
        // Enrichment Builders
        services.AddTransient<SocialMetricBuilder>();
        services.AddTransient<CrimeMetricBuilder>();
        services.AddTransient<DemographicsMetricBuilder>();
        services.AddTransient<AmenityMetricBuilder>();
        services.AddTransient<EnvironmentMetricBuilder>();
        services.AddTransient<ScoringCalculator>();

        // Configuration
        services.Configure<JwtOptions>(options => BindJwtOptions(options, configuration));
        services.Configure<ContextEnrichmentOptions>(options => BindContextEnrichmentOptions(options, configuration));
        services.AddHttpClient();
        services.AddHttpClient<IPdokListingService, PdokListingService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
        });
        services.AddHttpClient<ILocationResolver, PdokLocationResolver>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
        });
        services.AddHttpClient<ICbsNeighborhoodStatsClient, CbsNeighborhoodStatsClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
        });
        services.AddHttpClient<IAmenityClient, OverpassAmenityClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        services.AddHttpClient<IAirQualityClient, LuchtmeetnetAirQualityClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        services.AddHttpClient<ICbsCrimeStatsClient, CbsCrimeStatsClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
        });
        services.AddHttpClient<IDemographicsClient, CbsDemographicsClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
        });


        return services;
    }

    private static void BindJwtOptions(JwtOptions options, IConfiguration configuration)
    {
        var secret = configuration["JWT_SECRET"];

        if (string.IsNullOrEmpty(secret))
        {
            throw new InvalidOperationException("JWT Secret is missing in configuration. Please set JWT_SECRET.");
        }

        options.Secret = secret;
        options.Issuer = configuration["JWT_ISSUER"];
        options.Audience = configuration["JWT_AUDIENCE"];

        if (double.TryParse(configuration["JWT_EXPIRY_MINUTES"], out var expiry))
        {
            options.ExpiryMinutes = expiry;
        }
    }

    private static void BindContextEnrichmentOptions(ContextEnrichmentOptions options, IConfiguration configuration)
    {
        options.PdokBaseUrl = configuration["CONTEXT_PDOK_BASE_URL"] ?? options.PdokBaseUrl;
        options.CbsBaseUrl = configuration["CONTEXT_CBS_BASE_URL"] ?? options.CbsBaseUrl;
        options.OverpassBaseUrl = configuration["CONTEXT_OVERPASS_BASE_URL"] ?? options.OverpassBaseUrl;
        options.LuchtmeetnetBaseUrl = configuration["CONTEXT_LUCHTMEETNET_BASE_URL"] ?? options.LuchtmeetnetBaseUrl;

        if (int.TryParse(configuration["CONTEXT_RESOLVER_CACHE_MINUTES"], out var resolverMinutes))
        {
            options.ResolverCacheMinutes = resolverMinutes;
        }

        if (int.TryParse(configuration["CONTEXT_CBS_CACHE_MINUTES"], out var cbsMinutes))
        {
            options.CbsCacheMinutes = cbsMinutes;
        }

        if (int.TryParse(configuration["CONTEXT_AMENITIES_CACHE_MINUTES"], out var amenitiesMinutes))
        {
            options.AmenitiesCacheMinutes = amenitiesMinutes;
        }

        if (int.TryParse(configuration["CONTEXT_AIR_CACHE_MINUTES"], out var airMinutes))
        {
            options.AirQualityCacheMinutes = airMinutes;
        }

        if (int.TryParse(configuration["CONTEXT_REPORT_CACHE_MINUTES"], out var reportMinutes))
        {
            options.ReportCacheMinutes = reportMinutes;
        }

        if (int.TryParse(configuration["CONTEXT_PDOK_LISTING_CACHE_MINUTES"], out var pdokListingMinutes))
        {
            options.PdokListingCacheMinutes = pdokListingMinutes;
        }
    }
}
