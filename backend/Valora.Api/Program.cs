using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Valora.Api.Endpoints;
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

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
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
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var configOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

        // 1. Filter config origins (remove nulls/whitespace, trim)
        var validOrigins = configOrigins.Where(o => !string.IsNullOrWhiteSpace(o)).Select(o => o.Trim()).ToList();

        // 2. If empty, try environment variable
        if (validOrigins.Count == 0)
        {
            var envOrigins = builder.Configuration["ALLOWED_ORIGINS"];
            if (!string.IsNullOrEmpty(envOrigins))
            {
                validOrigins.AddRange(envOrigins.Split(",", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            }
        }

        // 3. Configure policy
        if (validOrigins.Count > 0)
        {
            policy.WithOrigins(validOrigins.ToArray())
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
        else
        {
            // Fallback logic
            if (builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing"))
            {
                policy.WithOrigins("http://localhost:3000")
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            }
            else
            {
                // In production, default to allowing all origins if not explicitly configured.
                // This prevents startup crashes while maintaining functionality for mobile apps.
                // A warning is logged at startup if this fallback is active.
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
        }
        }
    });
});
var app = builder.Build();

// Log warning if CORS is insecurely configured in production
if (app.Environment.IsProduction())
{
    var configOrigins = app.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
    var envOrigins = app.Configuration["ALLOWED_ORIGINS"];

    // Using the same logic as AddCors to determine effective configuration
    var hasValidConfig = configOrigins.Any(o => !string.IsNullOrWhiteSpace(o)) || !string.IsNullOrWhiteSpace(envOrigins);

    if (!hasValidConfig)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogWarning("SECURITY WARNING: CORS AllowedOrigins not configured. Defaulting to AllowAnyOrigin. This is insecure. Configure AllowedOrigins or ALLOWED_ORIGINS to restrict access.");
    }
}

// Apply database migrations
if (!app.Environment.IsEnvironment("Testing"))
{
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
        try
        {
            if (dbContext.Database.IsRelational())
            {
                dbContext.Database.Migrate();
            }

            // Seed Admin User
            var adminEmail = app.Configuration["ADMIN_EMAIL"];
            var adminPassword = app.Configuration["ADMIN_PASSWORD"];

            if (!string.IsNullOrEmpty(adminEmail) && !string.IsNullOrEmpty(adminPassword))
            {
                var identityService = scope.ServiceProvider.GetRequiredService<IIdentityService>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

                await identityService.EnsureRoleAsync("Admin");
                
                var user = await identityService.GetUserByEmailAsync(adminEmail);
                if (user == null)
                {
                    // Only create and promote if user does NOT exist (prevents takeover of existing accounts)
                    var (createResult, userId) = await identityService.CreateUserAsync(adminEmail, adminPassword);
                    if (createResult.Succeeded)
                    {
                        var roleResult = await identityService.AddToRoleAsync(userId, "Admin");
                        if (roleResult.Succeeded)
                        {
                            logger.LogInformation("Successfully seeded initial Admin user.");
                        }
                        else
                        {
                            logger.LogWarning("Created Admin user but failed to assign role. Check identity logs.");
                        }
                    }
                    else
                    {
                        logger.LogWarning("Failed to create initial Admin user. Check identity logs.");
                    }
                }
                else
                {
                    logger.LogWarning("Admin seeding: User configured in ADMIN_EMAIL already exists. Skipping automatic promotion to prevent privilege escalation.");
                }
            }
        }
        catch (Exception ex)
        {
            // Log error or handle it (e.g., if database is not ready yet)
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred while migrating the database.");
        }
    }
}

if (app.Environment.IsProduction() || app.Configuration.GetValue<bool>("ENABLE_HTTPS_REDIRECTION"))
{
    if (app.Environment.IsProduction())
    {
        app.UseHsts();
    }
    app.UseHttpsRedirection();
}

app.UseMiddleware<Valora.Api.Middleware.ExceptionHandlingMiddleware>();
app.UseMiddleware<Valora.Api.Middleware.SecurityHeadersMiddleware>();

app.UseCors();
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

// Map Auth Endpoints (Injects IConfiguration into handler)
app.MapAuthEndpoints();
app.MapNotificationEndpoints();
app.MapAiEndpoints();
app.MapMapEndpoints();

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
api.MapGet("/listings", async ([AsParameters] ListingFilterDto filter, IListingRepository repo, CancellationToken ct) =>
{
    var validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(filter);
    var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();

    if (!System.ComponentModel.DataAnnotations.Validator.TryValidateObject(filter, validationContext, validationResults, true))
    {
        return Results.BadRequest(validationResults.Select(r => new { Property = r.MemberNames.FirstOrDefault(), Error = r.ErrorMessage }));
    }

    var paginatedList = await repo.GetSummariesAsync(filter, ct);

    return Results.Ok(new
    {
        paginatedList.Items,
        paginatedList.PageIndex,
        paginatedList.TotalPages,
        paginatedList.TotalCount,
        paginatedList.HasNextPage,
        paginatedList.HasPreviousPage
    });
})
.RequireAuthorization();

/// <summary>
/// Looks up property details from PDOK by ID and enriches with neighborhood analytics.
/// Requires Authentication.
/// </summary>
api.MapGet("/listings/lookup", async (string id, IPdokListingService pdokService, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(id)) return Results.BadRequest("ID is required");
    
    var listing = await pdokService.GetListingDetailsAsync(id, ct);
    if (listing is null) return Results.NotFound();

    return Results.Ok(listing);
})
.RequireAuthorization()
.RequireRateLimiting("strict");

/// <summary>
/// Retrieves detailed information for a specific listing by ID.
/// Requires Authentication.
/// </summary>
api.MapGet("/listings/{id:guid}", async (Guid id, IListingRepository repo, CancellationToken ct) =>
{
    var listing = await repo.GetByIdAsync(id, ct);
    if (listing is null) return Results.NotFound();
    
    var dto = ListingMapper.ToDto(listing);
    return Results.Ok(dto);
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
    IListingRepository repo,
    IContextReportService contextReportService,
    CancellationToken ct) =>
{
    var listing = await repo.GetByIdAsync(id, ct);
    if (listing is null) return Results.NotFound();

    // 1. Generate Report
    ContextReportRequestDto request = new(Input: listing.Address); // Default 1km radius
    
    // We use the application DTO for the service call...
    var reportDto = await contextReportService.BuildAsync(request, ct);

    // ...and map it to the Domain model for storage
    var contextReportModel = ListingMapper.MapToDomain(reportDto);

    // 2. Update Entity
    listing.ContextReport = contextReportModel;
    listing.ContextCompositeScore = reportDto.CompositeScore;

    if (reportDto.CategoryScores.TryGetValue("Social", out var social)) listing.ContextSocialScore = social;
    if (reportDto.CategoryScores.TryGetValue("Safety", out var crime)) listing.ContextSafetyScore = crime; // Mapping "Safety" to "Safety" score
    if (reportDto.CategoryScores.TryGetValue("Demographics", out var demo)) { /* No specific column yet */ }
    if (reportDto.CategoryScores.TryGetValue("Amenities", out var amenities)) listing.ContextAmenitiesScore = amenities;
    if (reportDto.CategoryScores.TryGetValue("Environment", out var env)) listing.ContextEnvironmentScore = env;

    // 3. Save
    await repo.UpdateAsync(listing, ct);

    return Results.Ok(new { message = "Listing enriched successfully", compositeScore = reportDto.CompositeScore });
})
.RequireAuthorization("Admin")
.RequireRateLimiting("strict");

app.Run();

public partial class Program { }
