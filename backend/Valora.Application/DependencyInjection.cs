using Microsoft.Extensions.DependencyInjection;

namespace Valora.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Add application services here (e.g., MediatR, FluentValidation)
        return services;
    }
}
