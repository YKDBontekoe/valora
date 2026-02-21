using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Valora.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Valora.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Valora.Application.Common.Interfaces;
using Moq;
using Valora.Infrastructure.Services.External;

namespace Valora.IntegrationTests;

public class IntegrationTestWebAppFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;
    public Mock<IAmenityClient> AmenityClientMock { get; } = new();
    public Mock<ICbsGeoClient> CbsGeoClientMock { get; } = new();
    public Mock<IGoogleTokenValidator> GoogleTokenValidatorMock { get; } = new();

    public IntegrationTestWebAppFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "AllowedOrigins:0", "http://localhost" },
                { "DATABASE_URL", _connectionString },
                { "JWT_SECRET", "TestSecretKeyForIntegrationTestingOnly123!" },
                { "JWT_ISSUER", "ValoraTest" },
                { "JWT_AUDIENCE", "ValoraTest" },
                { "SENTRY_DSN", "https://d7879133400742199b24471545465c4a@o123456.ingest.sentry.io/123456" },
                { "SENTRY_TRACES_SAMPLE_RATE", "1.0" },
                { "SENTRY_PROFILES_SAMPLE_RATE", "0.0" },
                { "JWT_EXPIRY_MINUTES", "15" }
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Remove existing DbContext registrations
            services.RemoveAll<DbContextOptions<ValoraDbContext>>();
            services.RemoveAll<DbContextOptions>();
            services.RemoveAll<ValoraDbContext>();

            var configType = Type.GetType("Microsoft.EntityFrameworkCore.Infrastructure.IDbContextOptionsConfiguration`1, Microsoft.EntityFrameworkCore");
            if (configType != null)
            {
                 var genericConfigType = configType.MakeGenericType(typeof(ValoraDbContext));
                 services.RemoveAll(genericConfigType);
            }

            var configServices = services.Where(d => d.ServiceType.Name.Contains("IDbContextOptionsConfiguration") &&
                                                     d.ServiceType.IsGenericType &&
                                                     d.ServiceType.GetGenericArguments()[0] == typeof(ValoraDbContext)).ToList();
            foreach (var s in configServices) services.Remove(s);

            var npgsqlServices = services.Where(d =>
                d.ServiceType.FullName?.Contains("Npgsql") == true ||
                d.ImplementationType?.FullName?.Contains("Npgsql") == true).ToList();

            foreach (var s in npgsqlServices)
            {
                services.Remove(s);
            }

            // Add DbContext
            if (_connectionString.StartsWith("InMemory"))
            {
                var dbName = _connectionString.Contains(":") ? _connectionString.Split(':')[1] : "ValoraIntegrationTestDb";
                services.AddDbContext<ValoraDbContext>(options =>
                    options.UseInMemoryDatabase(dbName)
                           .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));
            }
            else
            {
                services.AddDbContext<ValoraDbContext>(options =>
                    options.UseNpgsql(_connectionString)
                           .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));
            }

            services.AddIdentityCore<ApplicationUser>()
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<ValoraDbContext>();

            // Mock external map clients
            services.RemoveAll<IAmenityClient>();
            services.AddSingleton<IAmenityClient>(AmenityClientMock.Object);

            services.RemoveAll<ICbsGeoClient>();
            services.AddSingleton<ICbsGeoClient>(CbsGeoClientMock.Object);

            // Mock Google Validator
            services.RemoveAll<IGoogleTokenValidator>();
            services.AddSingleton<IGoogleTokenValidator>(GoogleTokenValidatorMock.Object);
        });
    }
}
