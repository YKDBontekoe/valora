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
        var connectionString = configuration["DATABASE_URL"] ?? configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<ValoraDbContext>(options =>
        {
            if (configuration.GetValue<bool>("UseInMemoryDatabase"))
            {
                options.UseInMemoryDatabase("ValoraDb");
            }
            else
            {
                options.UseNpgsql(
                    connectionString,
                    npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorCodesToAdd: null));
            }
        });

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
