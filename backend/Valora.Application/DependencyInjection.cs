using Microsoft.Extensions.DependencyInjection;
using Valora.Application.Common.Interfaces;

namespace Valora.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Currently empty as all Application services logic has been moved to Infrastructure
        // Application layer should only declare abstract Interfaces/DTOs.
        return services;
    }
}
