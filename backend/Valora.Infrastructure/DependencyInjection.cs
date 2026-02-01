using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Valora.Application.Common.Interfaces;
using Valora.Application.Scraping;
using Valora.Infrastructure.Jobs;
using Valora.Infrastructure.Persistence;
using Valora.Infrastructure.Persistence.Repositories;
using Valora.Infrastructure.Scraping;

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

        // Scraper configuration
        services.Configure<ScraperOptions>(options =>
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
        });

        // Scraper services
        services.AddHttpClient<IFundaScraperService, FundaScraperService>();
        services.AddScoped<FundaScraperJob>();

        return services;
    }
}
