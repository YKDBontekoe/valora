using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Valora.Application.Common.Interfaces;
using Valora.Application.Common.Models;
using Valora.Application.Scraping;
using Valora.Application.Services;
using Valora.Infrastructure.Jobs;
using Valora.Infrastructure.Persistence;
using Valora.Infrastructure.Persistence.Repositories;
using Valora.Infrastructure.Scraping;
using Valora.Infrastructure.Services;
using Polly;
using Polly.Extensions.Http;

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
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IAiService, OpenRouterAiService>();
        services.AddScoped<INotificationService, NotificationService>();

        // Configuration
        services.Configure<ScraperOptions>(options => BindScraperOptions(options, configuration));
        services.Configure<JwtOptions>(options => BindJwtOptions(options, configuration));

        // Scraper services
        // Choose between Playwright (browser automation) and HTTP client.
        // Default is false for broader environment compatibility (e.g. containers without browser deps).
        var usePlaywright = configuration.GetValue("SCRAPER_USE_PLAYWRIGHT", false);

        if (usePlaywright)
        {
            // Playwright-based client for environments with bot protection
            services.AddSingleton<IFundaApiClient, PlaywrightFundaClient>();
        }
        else
        {
            // HTTP-based client (faster, but may get 403 from Funda)
            services.AddHttpClient<IFundaApiClient, FundaApiClient>()
                .SetHandlerLifetime(TimeSpan.FromMinutes(5))
                .ConfigureHttpClient(c => c.Timeout = TimeSpan.FromSeconds(30))
                .AddPolicyHandler((serviceProvider, _) =>
                    HttpPolicyExtensions
                        .HandleTransientHttpError()
                        .WaitAndRetryAsync(
                        3,
                        retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                        onRetry: (outcome, timespan, retryAttempt, context) =>
                        {
                            var logger = serviceProvider.GetRequiredService<ILogger<FundaApiClient>>();
                            logger.LogWarning(
                                outcome.Exception,
                                "Retrying Funda API request. Attempt {RetryAttempt}. DelaySeconds {DelaySeconds}. StatusCode {StatusCode}",
                                retryAttempt,
                                timespan.TotalSeconds,
                                outcome.Result?.StatusCode);
                        }));
        }

        services.AddScoped<IFundaScraperService, FundaScraperService>();
        services.AddScoped<IFundaSearchService, FundaSearchService>();
        services.AddScoped<FundaScraperJob>();


        return services;
    }

    private static void BindScraperOptions(ScraperOptions options, IConfiguration configuration)
    {
        var searchUrls = configuration["SCRAPER_SEARCH_URLS"];
        if (!string.IsNullOrEmpty(searchUrls))
        {
            options.SearchUrls = searchUrls.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        if (int.TryParse(configuration["SCRAPER_DELAY_MS"], out var delay))
        {
            options.DelayBetweenRequestsMs = delay;
        }

        if (int.TryParse(configuration["SCRAPER_MAX_RETRIES"], out var retries))
        {
            options.MaxRetries = retries;
        }

        var cron = configuration["SCRAPER_CRON"];
        if (!string.IsNullOrEmpty(cron))
        {
            options.CronExpression = cron;
        }
    }

    private static void BindJwtOptions(JwtOptions options, IConfiguration configuration)
    {
        options.Secret = configuration["JWT_SECRET"] ?? string.Empty;
        options.Issuer = configuration["JWT_ISSUER"];
        options.Audience = configuration["JWT_AUDIENCE"];

        if (double.TryParse(configuration["JWT_EXPIRY_MINUTES"], out var expiry))
        {
            options.ExpiryMinutes = expiry;
        }
    }
}
