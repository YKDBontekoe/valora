using Microsoft.Extensions.DependencyInjection;
using Valora.Application.Common.Interfaces;
using Valora.Application.Scraping;
using Valora.Application.Services;

namespace Valora.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();

        services.AddMemoryCache();
        services.AddScoped<IFundaScraperService, FundaScraperService>();
        services.AddScoped<IFundaSearchService, FundaSearchService>();

        return services;
    }
}
