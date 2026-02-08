using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Valora.Application.Common.Interfaces;
using Valora.Application.Common.Models;
using Valora.Application.Enrichment;
using Valora.Application.Services;
using Valora.Infrastructure.Enrichment;
using Valora.Infrastructure.Persistence;
using Valora.Infrastructure.Persistence.Repositories;
using Valora.Infrastructure.Services;
using Polly;

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

        // Configuration
        services.Configure<JwtOptions>(options => BindJwtOptions(options, configuration));
        services.Configure<ContextEnrichmentOptions>(options => BindContextEnrichmentOptions(options, configuration));
        services.AddHttpClient();
        services.AddHttpClient<ILocationResolver, PdokLocationResolver>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
        })
        .AddTransientHttpErrorPolicy(builder =>
            builder.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));

        services.AddHttpClient<ICbsNeighborhoodStatsClient, CbsNeighborhoodStatsClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
        })
        .AddTransientHttpErrorPolicy(builder =>
            builder.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));

        services.AddHttpClient<IAmenityClient, OverpassAmenityClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .AddTransientHttpErrorPolicy(builder =>
            builder.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));

        services.AddHttpClient<IAirQualityClient, LuchtmeetnetAirQualityClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .AddTransientHttpErrorPolicy(builder =>
            builder.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));


        return services;
    }

    private static void BindJwtOptions(JwtOptions options, IConfiguration configuration)
    {
        var secret = configuration["JWT_SECRET"];

        // Fallback for Development environment to match Program.cs behavior
        if (string.IsNullOrEmpty(secret))
        {
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            if (string.Equals(env, "Development", StringComparison.OrdinalIgnoreCase))
            {
                secret = "DevSecretKey_ChangeMe_In_Production_Configuration_123!";
            }
        }

        options.Secret = secret ?? string.Empty;
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
    }
}
