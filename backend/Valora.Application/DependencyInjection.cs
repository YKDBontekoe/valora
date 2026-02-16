using Microsoft.Extensions.DependencyInjection;
using Valora.Application.Common.Interfaces;
using Valora.Application.Services;

namespace Valora.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IContextReportService, ContextReportService>();
        services.AddScoped<IContextDataProvider, ContextDataProvider>();
        services.AddScoped<IListingService, ListingService>();
        services.AddScoped<IMapService, MapService>();
        return services;
    }
}
