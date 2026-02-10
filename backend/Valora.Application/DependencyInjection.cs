using Microsoft.Extensions.DependencyInjection;
using Valora.Application.Common.Interfaces;
using Valora.Application.Enrichment;
using Valora.Application.Enrichment.Builders;
using Valora.Application.Services;

namespace Valora.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IContextReportService, ContextReportService>();

        // Enrichment Builders
        services.AddTransient<SocialMetricBuilder>();
        services.AddTransient<CrimeMetricBuilder>();
        services.AddTransient<DemographicsMetricBuilder>();
        services.AddTransient<AmenityMetricBuilder>();
        services.AddTransient<EnvironmentMetricBuilder>();
        services.AddTransient<ScoringCalculator>();

        return services;
    }
}
