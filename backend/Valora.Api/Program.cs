using System.Security.Claims;
using Sentry;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Valora.Api.Endpoints;
using Valora.Api.Extensions;
using Valora.Api.Middleware;
using Valora.Application;
using Valora.Application.Common.Exceptions;
using Valora.Application.Common.Interfaces;
using Valora.Application.Common.Mappings;
using Valora.Application.DTOs;
using Valora.Infrastructure;
using Valora.Infrastructure.Persistence;
using Valora.Domain.Entities;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
// Configure Sentry
var sentryDsn = builder.Configuration["SENTRY_DSN"];
if (!string.IsNullOrEmpty(sentryDsn))
{
    builder.WebHost.UseSentry(options =>
    {
        options.Dsn = sentryDsn;

        // TracesSampleRate is read from configuration, defaulting to 1.0 if not specified.
        options.TracesSampleRate = builder.Configuration.GetValue<double>("SENTRY_TRACES_SAMPLE_RATE", 1.0);

        // Enable Sentry SDK debug mode in development
        options.Debug = builder.Environment.IsDevelopment();

        // Set Environment based on ASPNETCORE_ENVIRONMENT
        options.Environment = builder.Environment.EnvironmentName;

        // Set Release from assembly version or environment variable
        options.Release = builder.Configuration["SENTRY_RELEASE"]
            ?? System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version?.ToString();

        // Add breadcrumbs for logger messages of level Information and higher
        options.MinimumBreadcrumbLevel = LogLevel.Information;

        // Capture events for logger messages of level Error and higher
        options.MinimumEventLevel = LogLevel.Error;

        // Disable request body capture to avoid leaking sensitive data
        options.MaxRequestBodySize = Sentry.Extensibility.RequestSize.None;

        // Do not send PII to Sentry
        options.SendDefaultPii = false;

        // Enable stacktrace attachment
        options.AttachStacktrace = true;

        // Configure Profiling
        options.ProfilesSampleRate = builder.Configuration.GetValue<double>("SENTRY_PROFILES_SAMPLE_RATE", 1.0);
        options.AddProfilingIntegration();
    });
}
builder.Services.AddSwaggerGen(option =>
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

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

// Add Identity
builder.Services.AddIdentityCore<ApplicationUser>()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ValoraDbContext>();

// Add Authentication
builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        var secret = builder.Configuration["JWT_SECRET"]
                     ?? throw new InvalidOperationException("JWT_SECRET is not configured.");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JWT_ISSUER"],
            ValidAudience = builder.Configuration["JWT_AUDIENCE"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                // logger.LogError(context.Exception, "Authentication failed");
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                // logger.LogDebug("Token validated for: {User}", context.Principal?.Identity?.Name);
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                // logger.LogWarning("OnChallenge: {Error}, {ErrorDescription}", context.Error, context.ErrorDescription);
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    var isTesting = builder.Environment.IsEnvironment("Testing");
    var permitLimitStrict = isTesting ? 1000 : 10;
    var permitLimitFixed = isTesting ? 1000 : 100;

    // Strict policy for sensitive/expensive endpoints (Auth, AI, Reports)
    options.AddFixedWindowLimiter("strict", limiterOptions =>
    {
        limiterOptions.PermitLimit = permitLimitStrict;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 0;
    });

    // General policy for standard API usage
    options.AddFixedWindowLimiter("fixed", limiterOptions =>
    {
        limiterOptions.PermitLimit = permitLimitFixed;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 2;
    });
});

// Add CORS for Flutter
builder.Services.AddCustomCors(builder.Configuration, builder.Environment);
var app = builder.Build();

app.UseCors();

app.UseMiddleware<Valora.Api.Middleware.ExceptionHandlingMiddleware>();

// Log warning if CORS is insecurely configured in production
app.LogCorsWarning();

// Apply database migrations
await DbInitializer.InitializeAsync(app.Services, app.Configuration, app.Environment);

// Only use HTTPS redirection if specifically enabled and NOT on Render
// Render handles SSL termination and redirection at the load balancer level.
var isOnRender = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("RENDER"));
if (!isOnRender && (app.Environment.IsProduction() || app.Configuration.GetValue<bool>("ENABLE_HTTPS_REDIRECTION")))
{
    if (app.Environment.IsProduction())
    {
        app.UseHsts();
    }
    app.UseHttpsRedirection();
}

app.UseMiddleware<Valora.Api.Middleware.SecurityHeadersMiddleware>();

app.UseRateLimiter();

app.UseAuthentication();

// Sentry User Enrichment Middleware
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        var user = new SentryUser
        {
            Id = context.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value,
            Email = context.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value,
            Username = context.User.Identity?.Name
        };

        SentrySdk.ConfigureScope(scope => scope.User = user);
    }
    await next();
});

app.UseAuthorization();

// Map Auth Endpoints (Injects IConfiguration into handler)
app.MapAuthEndpoints();
app.MapNotificationEndpoints();
app.MapAiEndpoints();
app.MapMapEndpoints();
app.MapAdminEndpoints();

// API Endpoints
var api = app.MapGroup("/api").RequireRateLimiting("fixed");

/// <summary>
/// Health check endpoint. Used by Docker Compose and load balancers.
/// </summary>
api.MapGet("/health", async (ValoraDbContext db, CancellationToken ct) =>
{
    if (await db.Database.CanConnectAsync(ct))
    {
        return Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }

    return Results.Problem("Service unavailable", statusCode: 503);
});

/// <summary>
/// Retrieves a paginated list of listings based on filter criteria.
/// Requires Authentication.
/// </summary>
api.MapGet("/listings", async ([AsParameters] ListingFilterDto filter, IListingService service, CancellationToken ct) =>
{
    try
    {
        var paginatedList = await service.GetListingsAsync(filter, ct);

        return Results.Ok(new
        {
            paginatedList.Items,
            paginatedList.PageIndex,
            paginatedList.TotalPages,
            paginatedList.TotalCount,
            paginatedList.HasNextPage,
            paginatedList.HasPreviousPage
        });
    }
    catch (Valora.Application.Common.Exceptions.ValidationException ex)
    {
        // Maintain legacy error format for backward compatibility
        // Format: [{ "property": "PropertyName", "error": "ErrorMessage" }]
        var legacyErrors = ex.Errors.SelectMany(kvp =>
            kvp.Value.Select(error => new { Property = kvp.Key, Error = error }));

        return Results.BadRequest(legacyErrors);
    }
})
.RequireAuthorization();

/// <summary>
/// Looks up property details from PDOK by ID and enriches with neighborhood analytics.
/// Requires Authentication.
/// </summary>
api.MapGet("/listings/lookup", async (string id, IListingService service, CancellationToken ct) =>
{
    var listing = await service.GetPdokListingAsync(id, ct);
    if (listing is null) return Results.NotFound();

    return Results.Ok(listing);
})
.RequireAuthorization()
.RequireRateLimiting("strict");

/// <summary>
/// Retrieves detailed information for a specific listing by ID.
/// Requires Authentication.
/// </summary>
api.MapGet("/listings/{id:guid}", async (Guid id, IListingService service, CancellationToken ct) =>
{
    var listing = await service.GetListingByIdAsync(id, ct);
    if (listing is null) return Results.NotFound();
    
    return Results.Ok(listing);
})
.RequireAuthorization();

api.MapPost("/context/report", async (
    ContextReportRequestDto request,
    IContextReportService contextReportService,
    CancellationToken ct) =>
{
    var report = await contextReportService.BuildAsync(request, ct);
    return Results.Ok(report);
})
.RequireAuthorization()
.RequireRateLimiting("strict")
.AddEndpointFilter<Valora.Api.Filters.ValidationFilter<ContextReportRequestDto>>();

api.MapPost("/listings/{id:guid}/enrich", async (
    Guid id,
    IListingService service,
    CancellationToken ct) =>
{
    var compositeScore = await service.EnrichListingAsync(id, ct);
    return Results.Ok(new { message = "Listing enriched successfully", compositeScore });
})
.RequireAuthorization("Admin")
.RequireRateLimiting("strict");

app.Run();

public partial class Program { }
