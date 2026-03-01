using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi.Models;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Valora.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCustomCors(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
                ConfigureCorsPolicy(policy, configuration, environment));
        });

        return services;
    }

    private static void ConfigureCorsPolicy(Microsoft.AspNetCore.Cors.Infrastructure.CorsPolicyBuilder policy, IConfiguration configuration, IHostEnvironment environment)
    {
        var configOrigins = configuration.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

        var validOrigins = configOrigins.Where(o => !string.IsNullOrWhiteSpace(o)).Select(o => o.Trim()).ToList();

        if (validOrigins.Count == 0)
        {
            var envOrigins = configuration["ALLOWED_ORIGINS"];
            if (!string.IsNullOrEmpty(envOrigins))
            {
                validOrigins.AddRange(envOrigins.Split(",", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            }
        }

        if (validOrigins.Count == 0)
        {
            var allowedHosts = configuration["ALLOWED_HOSTS"];
            if (allowedHosts == "*")
            {
                validOrigins.Add("*");
            }
        }

        if (validOrigins.Any(o => o == "*"))
        {
            policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
        }
        else if (validOrigins.Count > 0)
        {
            policy.WithOrigins(validOrigins.ToArray()).AllowAnyMethod().AllowAnyHeader();
        }
        else
        {
            if (environment.IsDevelopment() || environment.IsEnvironment("Testing"))
            {
                policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
            }
            else
            {
                policy.SetIsOriginAllowed(origin => false);
            }
        }
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

    public static IServiceCollection AddSwaggerConfig(this IServiceCollection services)
    {
        services.AddSwaggerGen(option =>
        {
            option.SwaggerDoc("v1", new OpenApiInfo { Title = "Valora API", Version = "v1" });
            option.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Description = "Please enter a valid token",
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                BearerFormat = "JWT",
                Scheme = "Bearer"
            });
            option.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    new string[] { }
                }
            });
        });

        return services;
    }


    public static IServiceCollection AddRateLimitingConfig(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            var isTesting = environment.IsEnvironment("Testing");
            var configStrict = configuration.GetValue<int?>("RateLimiting:StrictLimit");
            var configFixed = configuration.GetValue<int?>("RateLimiting:FixedLimit");
            var configStrictQueue = configuration.GetValue<int?>("RateLimiting:StrictQueueLimit");
            var configFixedQueue = configuration.GetValue<int?>("RateLimiting:FixedQueueLimit");
            var configAuth = configuration.GetValue<int?>("RateLimiting:AuthLimit");
            var permitLimitAuth = (configAuth.HasValue && configAuth.Value > 0) ? configAuth.Value : (isTesting ? 1000 : 20);
            var queueLimitStrict = (configStrictQueue.HasValue && configStrictQueue.Value >= 0) ? configStrictQueue.Value : 10;
            var queueLimitFixed = (configFixedQueue.HasValue && configFixedQueue.Value >= 0) ? configFixedQueue.Value : 20;

            // Validate configuration values. If invalid (<= 0), fallback to defaults.
            var permitLimitStrict = (configStrict.HasValue && configStrict.Value > 0) ? configStrict.Value : (isTesting ? 1000 : 1000);
            var permitLimitFixed = (configFixed.HasValue && configFixed.Value > 0) ? configFixed.Value : (isTesting ? 1000 : 10000);

            ConfigureAuthRateLimit(options, permitLimitAuth);
            ConfigureStrictRateLimit(options, permitLimitStrict, queueLimitStrict);
            ConfigureFixedRateLimit(options, permitLimitFixed, queueLimitFixed);
        });

        return services;
    }

    private static void ConfigureAuthRateLimit(RateLimiterOptions options, int permitLimit)
    {
        options.AddPolicy(Valora.Api.Constants.RateLimitPolicies.Auth, context =>
        {
            var partitionKey = GetPartitionKey(context);
            return RateLimitPartition.GetFixedWindowLimiter(partitionKey,
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = permitLimit,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                });
        });
    }

    private static void ConfigureStrictRateLimit(RateLimiterOptions options, int permitLimit, int queueLimit)
    {
        options.AddPolicy(Valora.Api.Constants.RateLimitPolicies.Strict, context =>
        {
            if (context.User.IsInRole("Admin"))
            {
                return RateLimitPartition.GetNoLimiter("Admin");
            }
            var partitionKey = GetPartitionKey(context);
            return RateLimitPartition.GetFixedWindowLimiter(partitionKey,
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = permitLimit,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = queueLimit
                });
        });
    }

    private static void ConfigureFixedRateLimit(RateLimiterOptions options, int permitLimit, int queueLimit)
    {
        options.AddPolicy(Valora.Api.Constants.RateLimitPolicies.Fixed, context =>
        {
            if (context.User.IsInRole("Admin"))
            {
                return RateLimitPartition.GetNoLimiter("Admin");
            }
            var partitionKey = GetPartitionKey(context);
            return RateLimitPartition.GetFixedWindowLimiter(partitionKey,
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = permitLimit,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = queueLimit
                });
        });
    }

    private static string GetPartitionKey(HttpContext context)
    {
        return context.User.Identity?.IsAuthenticated == true
            ? context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous"
            : context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
    }
}
