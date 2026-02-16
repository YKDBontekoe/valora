using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;

namespace Valora.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCustomCors(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                var configOrigins = configuration.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

                // 1. Filter config origins (remove nulls/whitespace, trim)
                var validOrigins = configOrigins.Where(o => !string.IsNullOrWhiteSpace(o)).Select(o => o.Trim()).ToList();

                // 2. If empty, try environment variables
                if (validOrigins.Count == 0)
                {
                    var envOrigins = configuration["ALLOWED_ORIGINS"];
                    if (!string.IsNullOrEmpty(envOrigins))
                    {
                        validOrigins.AddRange(envOrigins.Split(",", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                    }
                }

                // 3. Fallback to ALLOWED_HOSTS if still empty (common user expectation)
                if (validOrigins.Count == 0)
                {
                    var allowedHosts = configuration["ALLOWED_HOSTS"];
                    if (allowedHosts == "*")
                    {
                        validOrigins.Add("*");
                    }
                }

                // 4. Configure policy
                if (validOrigins.Any(o => o == "*"))
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                }
                else if (validOrigins.Count > 0)
                {
                    policy.WithOrigins(validOrigins.ToArray())
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                }
                else
                {
                    // Fallback logic
                    if (environment.IsDevelopment() || environment.IsEnvironment("Testing"))
                    {
                        // In development, if no origins are specified, allow any origin 
                        // to support various mobile emulators and local web ports.
                        policy.AllowAnyOrigin()
                              .AllowAnyMethod()
                              .AllowAnyHeader();
                    }
                    else
                    {
                        // In production, do NOT allow any origin by default.
                        // This forces explicit configuration.
                        // A warning will be logged at startup if no origins are configured.
                        policy.SetIsOriginAllowed(origin => false);
                    }
                }
            });
        });

        return services;
    }

    public static void LogCorsWarning(this WebApplication app)
    {
        if (app.Environment.IsProduction())
        {
            var configOrigins = app.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
            var envOrigins = app.Configuration["ALLOWED_ORIGINS"];

            // Using the same logic as AddCors to determine effective configuration
            var hasValidConfig = configOrigins.Any(o => !string.IsNullOrWhiteSpace(o)) || !string.IsNullOrWhiteSpace(envOrigins);

            if (!hasValidConfig)
            {
                var logger = app.Services.GetRequiredService<ILogger<Program>>();
                logger.LogWarning("SECURITY WARNING: CORS AllowedOrigins not configured. Defaulting to DenyAll. This is secure but may break clients. Configure AllowedOrigins or ALLOWED_ORIGINS to allow access.");
            }
        }
    }
}
