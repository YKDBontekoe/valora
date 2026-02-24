using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi.Models;
using Valora.Infrastructure.Persistence;
using Valora.Domain.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Identity;
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

    public static IServiceCollection AddIdentityAndAuth(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        // Add Identity
        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 12;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ValoraDbContext>();

        // Add Authentication
        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                var secret = configuration["JWT_SECRET"]
                             ?? throw new InvalidOperationException("JWT_SECRET is not configured.");

                if (!environment.IsDevelopment() && secret == "DevelopmentOnlySecret_DoNotUseInProd_ChangeMe!")
                {
                    throw new InvalidOperationException("Critical Security Risk: The application is running in a non-development environment with the default, insecure JWT_SECRET. You MUST override JWT_SECRET with a strong, random key in your environment variables.");
                }

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["JWT_ISSUER"],
                    ValidAudience = configuration["JWT_AUDIENCE"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<JwtBearerEvents>>();
                        logger.LogError(context.Exception, "Authentication failed.");
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<JwtBearerEvents>>();
                        logger.LogDebug("Token validated for user: {User}", context.Principal?.Identity?.Name);
                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<JwtBearerEvents>>();
                        logger.LogWarning("Authentication challenge: {Error}, {ErrorDescription}", context.Error, context.ErrorDescription);
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
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
            var queueLimitStrict = (configStrictQueue.HasValue && configStrictQueue.Value >= 0) ? configStrictQueue.Value : 10;
            var queueLimitFixed = (configFixedQueue.HasValue && configFixedQueue.Value >= 0) ? configFixedQueue.Value : 20;

            // Validate configuration values. If invalid (<= 0), fallback to defaults.
            var permitLimitStrict = (configStrict.HasValue && configStrict.Value > 0) ? configStrict.Value : (isTesting ? 1000 : 1000);
            var permitLimitFixed = (configFixed.HasValue && configFixed.Value > 0) ? configFixed.Value : (isTesting ? 1000 : 10000);

            // Policy: "strict"
            options.AddPolicy("strict", context =>
            {
                if (context.User.IsInRole("Admin"))
                {
                    return RateLimitPartition.GetNoLimiter("Admin");
                }

                var partitionKey = context.User.Identity?.IsAuthenticated == true
                    ? context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous"
                    : context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";

                return RateLimitPartition.GetFixedWindowLimiter(partitionKey,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = permitLimitStrict,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = queueLimitStrict
                    });
            });

            // Policy: "fixed"
            options.AddPolicy("fixed", context =>
            {
                if (context.User.IsInRole("Admin"))
                {
                    return RateLimitPartition.GetNoLimiter("Admin");
                }

                var partitionKey = context.User.Identity?.IsAuthenticated == true
                    ? context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous"
                    : context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";

                return RateLimitPartition.GetFixedWindowLimiter(partitionKey,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = permitLimitFixed,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = queueLimitFixed
                    });
            });
        });

        return services;
    }
}
