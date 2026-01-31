using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Valora.Application.Common.Interfaces;
using Valora.Application.Scraping;
using Valora.Infrastructure.Jobs;
using Valora.Infrastructure.Persistence;
using Valora.Infrastructure.Persistence.Repositories;
using Valora.Infrastructure.Scraping;
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

        // Scraper configuration
        services.Configure<ScraperOptions>(configuration.GetSection(ScraperOptions.SectionName));

        // Scraper services
        services.AddHttpClient<IFundaScraperService, FundaScraperService>();
        services.AddScoped<FundaScraperJob>();

        // Services
        services.AddScoped<IListingService, ListingService>();
        services.AddScoped<IJobScheduler, HangfireJobScheduler>();

        return services;
    }
}
