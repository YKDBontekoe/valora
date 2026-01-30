using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Valora.Application.Common.Interfaces;
using Valora.Infrastructure.Persistence;
using Valora.Infrastructure.Persistence.Repositories;

namespace Valora.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ValoraDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IListingRepository, ListingRepository>();

        return services;
    }
}
