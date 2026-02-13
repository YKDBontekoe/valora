using Microsoft.Extensions.DependencyInjection;
using Valora.Application.Common.Interfaces;
using Valora.Application.Services;

namespace Valora.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IContextReportService, ContextReportService>();
        services.AddScoped<IListingService, ListingService>();
        return services;
    }
}
