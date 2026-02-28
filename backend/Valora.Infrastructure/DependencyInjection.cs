using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Valora.Application.Common.Interfaces;
using Valora.Application.Common.Models;
using Valora.Application.Enrichment;
using Valora.Application.Services;
using Valora.Infrastructure.Enrichment;
using Valora.Infrastructure.Persistence;
using Valora.Infrastructure.Persistence.Repositories;
using Valora.Infrastructure.Services;
using Valora.Application.Common.Interfaces.External;
using Valora.Infrastructure.Services.External;
using System.Net;
using Microsoft.Extensions.Http.Resilience;
using Polly;

namespace Valora.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Ensure Logging is available for infrastructure services even in non-web contexts
        services.AddLogging();

        var rawConnectionString = configuration["DATABASE_URL"] ?? configuration.GetConnectionString("DefaultConnection");
        var connectionString = ConnectionStringParser.BuildConnectionString(rawConnectionString);

        services.AddDbContext<ValoraDbContext>(options =>
        {
            if (connectionString != null && connectionString.StartsWith("InMemory", StringComparison.OrdinalIgnoreCase))
                return;

            options.UseSqlServer(
                connectionString,
                sqlOptions => sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null));
            options.ConfigureWarnings(w => w.Log(RelationalEventId.PendingModelChangesWarning));
        });

        // Repositories
        services.AddScoped<IPriceHistoryRepository, PriceHistoryRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IBatchJobRepository, BatchJobRepository>();
        services.AddScoped<INeighborhoodRepository, NeighborhoodRepository>();
        services.AddScoped<IUserAiProfileRepository, UserAiProfileRepository>();
        services.AddScoped<IWorkspaceRepository, WorkspaceRepository>();

        // Services
        services.AddSingleton(TimeProvider.System);
        services.AddMemoryCache();
        services.AddSingleton<IRequestMetricsService, RequestMetricsService>();
        services.AddScoped<ISystemHealthService, SystemHealthService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IAiModelService, AiModelService>();
        services.AddScoped<IAiService, OpenRouterAiService>();
        services.AddSingleton<ICacheService, CacheService>();
        services.AddScoped<IMapRepository, MapRepository>();
        services.AddScoped<IExternalAuthService, ExternalAuthService>();
        services.AddScoped<IGoogleTokenValidator, GoogleTokenValidator>();
        
        // Configuration
        services.Configure<JwtOptions>(options => BindJwtOptions(options, configuration));
        services.Configure<ContextEnrichmentOptions>(options => BindContextEnrichmentOptions(options, configuration));
        services.AddHttpClient();

        services.AddHttpClient<ILocationResolver, PdokLocationResolver>()
        .AddStandardResilienceHandler(options => {
            options.Retry.MaxRetryAttempts = 3;
            options.Retry.Delay = TimeSpan.FromSeconds(2);
            options.Retry.BackoffType = DelayBackoffType.Exponential;
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(45);
        });

        services.AddHttpClient<ICbsNeighborhoodStatsClient, CbsNeighborhoodStatsClient>(client => { client.DefaultRequestVersion = HttpVersion.Version11; client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower; })
        .AddStandardResilienceHandler(options => {
            options.Retry.MaxRetryAttempts = 3;
            options.Retry.Delay = TimeSpan.FromSeconds(2);
            options.Retry.BackoffType = DelayBackoffType.Exponential;
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30);
        });

        services.AddHttpClient<IAmenityClient, OverpassAmenityClient>()
        .AddStandardResilienceHandler(options => {
            options.Retry.MaxRetryAttempts = 3;
            options.Retry.Delay = TimeSpan.FromSeconds(2);
            options.Retry.BackoffType = DelayBackoffType.Exponential;
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(45);
        });

        services.AddHttpClient<IAirQualityClient, LuchtmeetnetAirQualityClient>()
        .AddStandardResilienceHandler(options => {
            options.Retry.MaxRetryAttempts = 3;
            options.Retry.Delay = TimeSpan.FromSeconds(2);
            options.Retry.BackoffType = DelayBackoffType.Exponential;
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(45);
        });

        services.AddHttpClient<ICbsCrimeStatsClient, CbsCrimeStatsClient>(client => { client.DefaultRequestVersion = HttpVersion.Version11; client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower; })
        .AddStandardResilienceHandler(options => {
            options.Retry.MaxRetryAttempts = 3;
            options.Retry.Delay = TimeSpan.FromSeconds(2);
            options.Retry.BackoffType = DelayBackoffType.Exponential;
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30);
        });

        services.AddHttpClient<ICbsGeoClient, CbsGeoClient>()
        .AddStandardResilienceHandler(options => {
            options.Retry.MaxRetryAttempts = 3;
            options.Retry.Delay = TimeSpan.FromSeconds(2);
            options.Retry.BackoffType = DelayBackoffType.Exponential;
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(60);
        });


        services.AddHttpClient<IWozValuationService, WozValuationService>()
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            UseCookies = true,
            AutomaticDecompression = DecompressionMethods.All
        })
        .AddStandardResilienceHandler(options => {
            options.Retry.MaxRetryAttempts = 3;
            options.Retry.Delay = TimeSpan.FromSeconds(2);
            options.Retry.BackoffType = DelayBackoffType.Exponential;
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(45);
        });

        services.AddHttpClient("OpenRouter")
        .AddStandardResilienceHandler(options => {
            options.Retry.MaxRetryAttempts = 3;
            options.Retry.Delay = TimeSpan.FromSeconds(2);
            options.Retry.BackoffType = DelayBackoffType.Exponential;
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(60);
        });

        return services;
    }

    private static void BindJwtOptions(JwtOptions options, IConfiguration configuration)
    {
        var secret = configuration["JWT_SECRET"];

        if (string.IsNullOrWhiteSpace(secret))
        {
            throw new OptionsValidationException("JwtOptions", typeof(JwtOptions), new[] { "JWT_SECRET is not configured or is whitespace." });
        }

        if (secret.Trim().Length < 32)
        {
            throw new OptionsValidationException("JwtOptions", typeof(JwtOptions), new[] { "JWT_SECRET must be at least 32 characters long (trimmed)." });
        }

        options.Secret = secret.Trim();
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
