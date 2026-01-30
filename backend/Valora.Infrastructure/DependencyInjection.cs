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
        services.AddDbContext<ValoraDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        // Repositories
        services.AddScoped<IListingRepository, ListingRepository>();
        services.AddScoped<IPriceHistoryRepository, PriceHistoryRepository>();

        // Scraper configuration
        services.Configure<ScraperOptions>(configuration.GetSection(ScraperOptions.SectionName));

        // Scraper services
        services.AddHttpClient<IFundaScraperService, FundaScraperService>();
        services.AddScoped<FundaScraperJob>();

        return services;
    }
}
